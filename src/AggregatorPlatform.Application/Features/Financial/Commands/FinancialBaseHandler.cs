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
    protected readonly IAccountingEngine Accounting;
    protected readonly IWebhookService Webhooks;
    protected readonly IMapper Mapper;
    protected readonly ILogger Logger;

    protected FinancialBaseHandler(
        ITransactionRepository transactions,
        ISubscriptionRepository subscriptions,
        IPartnerRepository partners,
        IUnitOfWork uow,
        IAccountingEngine accounting,
        IWebhookService webhooks,
        IMapper mapper,
        ILogger logger)
    {
        Transactions = transactions;
        Subscriptions = subscriptions;
        Partners = partners;
        Uow = uow;
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
    /// Construit la transaction avec FeeAmount = override request.Fees si fourni, sinon 0
    /// (le AccountingEngine recalculera depuis les lignes IsFee du schema).
    /// </summary>
    protected Transaction BuildTransaction(
        TransactionRequest request,
        Subscription? subscription,
        Guid partnerId,
        TransactionType type)
    {
        var fee = request.Fees ?? 0m;
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
            BankAccount = request.BankAccount,
            PhoneNumber = request.PhoneNumber,
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
