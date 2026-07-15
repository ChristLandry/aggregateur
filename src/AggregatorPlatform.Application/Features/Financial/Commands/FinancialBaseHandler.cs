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
    protected readonly IPartnerEndpointRepository PartnerEndpoints;
    protected readonly IUnitOfWork Uow;
    protected readonly IAccountingEngine Accounting;
    protected readonly IWebhookService Webhooks;
    protected readonly IMapper Mapper;
    protected readonly ILogger Logger;

    protected FinancialBaseHandler(
        ITransactionRepository transactions,
        ISubscriptionRepository subscriptions,
        IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow,
        IAccountingEngine accounting,
        IWebhookService webhooks,
        IMapper mapper,
        ILogger logger)
    {
        Transactions = transactions;
        Subscriptions = subscriptions;
        Partners = partners;
        PartnerEndpoints = partnerEndpoints;
        Uow = uow;
        Accounting = accounting;
        Webhooks = webhooks;
        Mapper = mapper;
        Logger = logger;
    }

    /// <summary>
    /// Verifications PRE-validation : partenaire actif, eligibilite sur l'endpoint
    /// et schema comptable lie. Renvoie un Result.Failure si une garde echoue,
    /// null si tout est OK.
    /// </summary>
    protected async Task<Result<TransactionDto>?> PreValidatePartnerAsync(
        Guid partnerId, TransactionType type, CancellationToken ct)
    {
        // 1) Partenaire existant + ApiKey valide (partenaire actif).
        //    L'authentification via X-Partner-ApiKey est faite dans le middleware ;
        //    on re-verifie ici cote handler en cas d'usage hors middleware.
        var partner = await Partners.GetByIdAsync(partnerId, ct);
        if (partner is null)
            return Result<TransactionDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");
        if (partner.Status != PartnerStatus.Active)
            return Result<TransactionDto>.Failure("PARTNER_INACTIVE", "Partner is not active.");
        if (string.IsNullOrEmpty(partner.ApiKey))
            return Result<TransactionDto>.Failure("PARTNER_APIKEY_MISSING",
                "Partner has no API key configured.");

        // 2) Pour les 4 endpoints configurables, la liaison Partner-Endpoint doit exister
        //    et avoir un schema comptable attache. WalletCancel est exempte (derivee).
        if (TryMapToEndpointKey(type, out var key))
        {
            var link = await PartnerEndpoints.GetByPartnerAndKeyAsync(partnerId, key, ct);
            if (link is null)
                return Result<TransactionDto>.Failure("PARTNER_ENDPOINT_NOT_CONFIGURED",
                    $"Partner is not configured for endpoint {key}.");
            if (link.SchemaId is null)
                return Result<TransactionDto>.Failure("PARTNER_ENDPOINT_SCHEMA_MISSING",
                    $"No accounting schema attached to partner endpoint {key}.");
        }

        return null;
    }

    private static bool TryMapToEndpointKey(TransactionType type, out FinancialEndpointKey key)
    {
        switch (type)
        {
            case TransactionType.BankDebit:    key = FinancialEndpointKey.BankDebit;    return true;
            case TransactionType.BankCredit:   key = FinancialEndpointKey.BankCredit;   return true;
            case TransactionType.WalletDebit:  key = FinancialEndpointKey.WalletDebit;  return true;
            case TransactionType.WalletCredit: key = FinancialEndpointKey.WalletCredit; return true;
            default: key = default; return false;
        }
    }

    protected async Task<Result<TransactionDto>?> CheckIdempotenceAsync(Guid partnerId, string partnerRef, CancellationToken ct)
    {
        try
        {
            var existing = await Transactions.GetByPartnerRefAsync(partnerId, partnerRef, ct);
            if (existing is not null)
            {
                Logger.LogInformation("Idempotent hit: partner {Partner} ref {Ref}", partnerId, partnerRef);
                return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(existing));
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError("Idempotent hit: partner {Partner} exception {ex}", partnerId, ex);
            throw;
        }
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
