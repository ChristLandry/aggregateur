using System.Text.Json;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Application.Features.Financial.Commands;

public abstract class FinancialBaseHandler
{
    protected readonly ITransactionRepository Transactions;
    protected readonly ISubscriptionRepository Subscriptions;
    protected readonly IPartnerRepository Partners;
    protected readonly IUnitOfWork Uow;
    protected readonly IFeeCalculator FeeCalculator;
    protected readonly IAccountingEngine Accounting;
    protected readonly IWebhookService Webhooks;
    protected readonly IMapper Mapper;
    protected readonly ILogger Logger;

    protected FinancialBaseHandler(
        ITransactionRepository transactions,
        ISubscriptionRepository subscriptions,
        IPartnerRepository partners,
        IUnitOfWork uow,
        IFeeCalculator feeCalculator,
        IAccountingEngine accounting,
        IWebhookService webhooks,
        IMapper mapper,
        ILogger logger)
    {
        Transactions = transactions;
        Subscriptions = subscriptions;
        Partners = partners;
        Uow = uow;
        FeeCalculator = feeCalculator;
        Accounting = accounting;
        Webhooks = webhooks;
        Mapper = mapper;
        Logger = logger;
    }

    protected async Task<Result<TransactionDto>?> CheckIdempotenceAsync(Guid partnerId, string partnerRef, CancellationToken ct)
    {
        var existing = await Transactions.GetByPartnerRefAsync(partnerId, partnerRef, ct);
        if (existing is not null)
        {
            Logger.LogInformation("Idempotent hit: partner {Partner} ref {Ref}", partnerId, partnerRef);
            return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(existing));
        }
        return null;
    }

    /// <summary>
    /// Resout l'abonnement (optionnel) lie a la requete. BankAccount et PhoneNumber etant
    /// desormais obligatoires dans le payload, l'absence de subscription est un cas valide.
    /// Retourne (subscription?, errorCode?) :
    ///   - ok subscription : trouve, actif et appartenant au partenaire
    ///   - ok null         : aucun SubscriptionId fourni
    ///   - erreur          : SubscriptionId fourni mais invalide
    /// </summary>
    protected async Task<(Subscription? Subscription, string? ErrorCode)> ResolveSubscriptionAsync(
        TransactionRequest request, Guid partnerId, CancellationToken ct)
    {
        if (request.SubscriptionId is null)
            return (null, null);

        var sub = await Subscriptions.GetByIdAsync(request.SubscriptionId.Value, ct);
        if (sub is null) return (null, "SUBSCRIPTION_INVALID");
        if (sub.PartnerId != partnerId) return (null, "SUBSCRIPTION_INVALID");
        if (sub.Status != SubscriptionStatus.Active) return (null, "SUBSCRIPTION_INVALID");
        return (sub, null);
    }

    /// <summary>
    /// Construit la transaction en utilisant en priorite les valeurs du payload, puis celles de l'abonnement.
    /// Les frais sont surchargees par <see cref="TransactionRequest.Fees"/> si fourni, sinon calcules.
    /// </summary>
    protected async Task<Transaction> BuildTransactionAsync(
        TransactionRequest request,
        Subscription? subscription,
        Guid partnerId,
        TransactionType type,
        CancellationToken ct)
    {
        var fee = request.Fees ?? await FeeCalculator.CalculateAsync(partnerId, type, request.Amount, ct);

        // BankAccount et PhoneNumber sont valides en entree par le validator.
        var bankAccount = request.BankAccount;
        var phoneNumber = request.PhoneNumber;

        return new Transaction
        {
            PartnerTransactionRef = request.PartnerTransactionRef,
            PartnerId = partnerId,
            SubscriptionId = subscription?.SubscriptionId,
            CustomerId = subscription?.CustomerId,
            TransactionType = type,
            Amount = request.Amount,
            FeeAmount = fee,
            NetAmount = request.Amount - fee,
            Currency = request.Currency,
            Status = TransactionStatus.Pending,
            AccountingStatus = AccountingStatus.Pending,
            InitiatedAt = DateTime.UtcNow,
            BankAccount = bankAccount,
            PhoneNumber = phoneNumber,
            ExtraData = SerializeExtraData(request.ExtraData),
        };
    }

    protected static string? SerializeExtraData(JsonElement? extra)
    {
        if (extra is null) return null;
        if (extra.Value.ValueKind == JsonValueKind.Null || extra.Value.ValueKind == JsonValueKind.Undefined) return null;
        return extra.Value.GetRawText();
    }

    protected async Task FinalizeAsync(Transaction tx, string? externalRef, bool success, string? failureReason, CancellationToken ct)
    {
        tx.ExternalRef = externalRef;
        tx.Status = success ? TransactionStatus.Success : TransactionStatus.Failed;
        tx.FailureReason = failureReason;
        tx.CompletedAt = DateTime.UtcNow;
        Transactions.Update(tx);

        if (success)
        {
            await Accounting.ApplyAsync(tx, ct);
        }

        await Uow.SaveChangesAsync(ct);
        await Webhooks.EnqueueAsync(tx.PartnerId, tx.TransactionId, $"transaction.{(success ? "success" : "failed")}", new
        {
            tx.TransactionId,
            tx.PartnerTransactionRef,
            tx.Status,
            tx.Amount,
            tx.FeeAmount,
            tx.Currency,
            tx.ExternalRef
        }, ct);
    }
}
