using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Application.Features.Financial.Commands;

/// <summary>
/// Base handler des endpoints /api/v1/bank/* (debit + credit).
/// Regles specifiques :
///  - PartnerTransactionRef unique (hard-fail si doublon).
///  - Souscription active resolue via (PartnerId, PhoneNumber, BankAccount).
/// Herite de <see cref="FinancialBaseHandler"/> pour les mecaniques communes
/// (PreValidatePartnerAsync, BuildTransaction, FinalizeAsync).
/// </summary>
public abstract class BankBaseHandler : FinancialBaseHandler
{
    protected BankBaseHandler(
        ITransactionRepository transactions,
        ISubscriptionRepository subscriptions,
        IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow,
        IAccountingEngine accounting,
        IWebhookService webhooks,
        IMapper mapper,
        ILogger logger)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
    }

    /// <summary>
    /// Verifie qu'aucune transaction avec le meme PartnerTransactionRef n'existe deja
    /// pour ce partenaire. Renvoie un Result.Failure DUPLICATE_PARTNER_TRANSACTION_REF
    /// si un doublon est detecte (comportement dur, sans idempotence).
    /// </summary>
    protected async Task<Result<TransactionDto>?> EnsureNoDuplicatePartnerRefAsync(
        Guid partnerId, string partnerRef, CancellationToken ct)
    {
        var existing = await Transactions.GetByPartnerRefAsync(partnerId, partnerRef, ct);
        if (existing is null) return null;

        Logger.LogWarning("Duplicate PartnerTransactionRef rejected: partner {Partner} ref {Ref} status {Status}",
            partnerId, partnerRef, existing.Status);
        return Result<TransactionDto>.Failure("DUPLICATE_PARTNER_TRANSACTION_REF",
            $"A transaction with PartnerTransactionRef '{partnerRef}' already exists (status={existing.Status}). Duplicate references are rejected.");
    }

    /// <summary>
    /// Resoud la souscription bancaire ACTIVE d'un partenaire pour un couple (phoneNumber, bankAccount).
    /// </summary>
    protected async Task<(Subscription? Subscription, Result<TransactionDto>? Error)> ResolveBankSubscriptionOrFailAsync(
        Guid partnerId, string? phoneNumber, string? bankAccount, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(bankAccount))
            return (null, Result<TransactionDto>.Failure("SUBSCRIPTION_REQUIRED",
                "PhoneNumber and BankAccount are both required to identify the subscription."));

        var sub = await Subscriptions.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, phoneNumber, bankAccount, ct);
        if (sub is null)
            return (null, Result<TransactionDto>.Failure("SUBSCRIPTION_NOT_FOUND",
                "No active subscription found for the provided PhoneNumber and BankAccount pair."));

        return (sub, null);
    }

    /// <summary>
    /// Construit la Transaction pour un endpoint bank a partir du DTO d'initiation.
    /// Mappe les champs sans passer par TransactionRequest (qui n'est pas expose sur les endpoints bank).
    /// </summary>
    protected Transaction BuildBankTransaction(
        BankTransactionInitiateRequest request,
        Subscription subscription,
        Guid partnerId,
        TransactionType type)
    {
        var fee = request.Fees ?? 0m;
        return new Transaction
        {
            PartnerTransactionRef = request.PartnerTransactionRef,
            PartnerId = partnerId,
            SubscriptionId = subscription.SubscriptionId,
            CustomerId = subscription.CustomerId,
            TransactionType = type,
            Amount = request.Amount,
            FeeAmount = fee,
            NetAmount = request.Amount - fee,
            Currency = request.Currency,
            Status = TransactionStatus.Pending,
            AccountingStatus = AccountingStatus.Pending,
            InitiatedAt = DateTime.UtcNow,
            BankAccount = request.BankAccount,
            PhoneNumber = request.PhoneNumber,
            ExtraData = SerializeExtraData(request.ExtraData),
            OperationType = request.OperationType,
        };
    }
}
