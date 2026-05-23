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

    protected async Task<Subscription?> EnsureActiveSubscriptionAsync(Guid subscriptionId, Guid partnerId, CancellationToken ct)
    {
        var sub = await Subscriptions.GetByIdAsync(subscriptionId, ct);
        if (sub is null) return null;
        if (sub.PartnerId != partnerId) return null;
        if (sub.Status != SubscriptionStatus.Active) return null;
        return sub;
    }

    protected async Task<Transaction> BuildTransactionAsync(
        TransactionRequest request,
        Subscription subscription,
        Guid partnerId,
        TransactionType type,
        CancellationToken ct)
    {
        var fee = await FeeCalculator.CalculateAsync(partnerId, type, request.Amount, ct);
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
            InitiatedAt = DateTime.UtcNow
        };
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

