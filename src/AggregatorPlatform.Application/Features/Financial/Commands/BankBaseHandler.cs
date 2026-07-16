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
/// Fournit un pipeline complet (partner check -> duplicate -> sub lookup -> balance ->
/// schema branch -> movements-first-if-hub-managed -> call bank -> persist status).
/// </summary>
public abstract class BankBaseHandler : FinancialBaseHandler
{
    protected readonly IAccountingSchemaRepository Schemas;
    protected readonly IBankApiClient BankClient;

    protected BankBaseHandler(
        ITransactionRepository transactions,
        ISubscriptionRepository subscriptions,
        IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IAccountingSchemaRepository schemas,
        IBankApiClient bankClient,
        IUnitOfWork uow,
        IAccountingEngine accounting,
        IWebhookService webhooks,
        IMapper mapper,
        ILogger logger)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        Schemas = schemas;
        BankClient = bankClient;
    }

    // ------------------------------------------------------------------
    // Blocs metier reutilises par debit + credit
    // ------------------------------------------------------------------

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
    /// Verifie le solde bancaire du client. Renvoie null si OK ou si la banque n'a
    /// pas pu confirmer (status != SUCCESS, tolere pour ne pas bloquer en dev). Sinon
    /// renvoie un Result.Failure INSUFFICIENT_FUNDS.
    /// </summary>
    protected async Task<Result<TransactionDto>?> EnsureSufficientBalanceAsync(
        Partner partner, string phoneNumber, decimal amount, decimal fees, CancellationToken ct)
    {
        var required = amount + fees;
        var balance = await BankClient.GetBalanceAsync(partner, phoneNumber, ct);
        if (!string.Equals(balance.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("Balance check tolerated: partner {Partner} phone {Phone} status={Status}",
                partner.PartnerId, phoneNumber, balance.Status);
            return null;
        }
        if (balance.Balance < required)
        {
            return Result<TransactionDto>.Failure("INSUFFICIENT_FUNDS",
                $"Client balance ({balance.Balance} {balance.Currency}) is below the required amount ({required} {balance.Currency}).");
        }
        return null;
    }

    protected async Task<AccountingSchema?> ResolveLinkedSchemaAsync(
        Guid partnerId, FinancialEndpointKey key, CancellationToken ct)
    {
        var link = await PartnerEndpoints.GetByPartnerAndKeyAsync(partnerId, key, ct);
        if (link?.SchemaId is null) return null;
        return await Schemas.GetByIdAsync(link.SchemaId.Value, ct);
    }

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

    // ------------------------------------------------------------------
    // Pipeline complet
    // ------------------------------------------------------------------

    protected async Task<Result<TransactionDto>> ProcessBankTransactionAsync(
        Guid partnerId,
        BankTransactionInitiateRequest req,
        TransactionType txType,
        FinancialEndpointKey key,
        bool isDebit,
        CancellationToken ct)
    {
        // 1) partenaire actif + endpoint configure + schema attache
        var pre = await PreValidatePartnerAsync(partnerId, txType, ct);
        if (pre is not null) return pre;

        // 2) ref d'idempotence unique
        var dup = await EnsureNoDuplicatePartnerRefAsync(partnerId, req.PartnerTransactionRef, ct);
        if (dup is not null) return dup;

        // 3) souscription (partner + phone + bank)
        var (sub, subErr) = await ResolveBankSubscriptionOrFailAsync(partnerId, req.PhoneNumber, req.BankAccount, ct);
        if (subErr is not null) return subErr;

        var partner = await Partners.GetByIdAsync(partnerId, ct);

        // 4) balance suffisant (debit uniquement — un credit alimente le compte cible)
        if (isDebit)
        {
            var balErr = await EnsureSufficientBalanceAsync(partner!, req.PhoneNumber!, req.Amount, req.Fees ?? 0m, ct);
            if (balErr is not null) return balErr;
        }

        // 5) schema comptable lie
        var schema = await ResolveLinkedSchemaAsync(partnerId, key, ct);
        if (schema is null)
            return Result<TransactionDto>.Failure("PARTNER_ENDPOINT_SCHEMA_MISSING",
                $"No accounting schema attached to partner endpoint {key}.");

        var tx = BuildBankTransaction(req, sub!, partnerId, txType);
        tx.SchemaId = schema.SchemaId;

        await Transactions.AddAsync(tx, ct);
        await Uow.SaveChangesAsync(ct);

        // 5b) hub-managed -> mouvements comptables AVANT l'appel bank
        if (!schema.IsBankManaged)
        {
            await Accounting.ApplyAsync(tx, ct);
            await Uow.SaveChangesAsync(ct);

            if (tx.AccountingStatus == AccountingStatus.Error)
            {
                tx.Status = TransactionStatus.Failed;
                tx.FailureReason = "Accounting schema evaluation failed.";
                tx.CompletedAt = DateTime.UtcNow;
                Transactions.Update(tx);
                await Uow.SaveChangesAsync(ct);
                return Result<TransactionDto>.Failure("ACCOUNTING_ERROR", tx.FailureReason);
            }
        }

        // 6) appel connecteur bancaire (bankAccount, codopsc, amount, fees)
        var codopsc = req.OperationType ?? schema.Name;
        var bankReq = new BankTransactionRequest(
            PartnerRef: tx.PartnerTransactionRef,
            BankAccount: req.BankAccount!,
            Codopsc: codopsc,
            Amount: tx.Amount,
            Fees: tx.FeeAmount,
            Currency: tx.Currency,
            Description: req.Description);

        BankTransactionResponse resp;
        try
        {
            resp = isDebit
                ? await BankClient.DebitAsync(partner!, bankReq, ct)
                : await BankClient.CreditAsync(partner!, bankReq, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Bank {Op} failed for tx {TxId}", isDebit ? "debit" : "credit", tx.TransactionId);
            tx.Status = TransactionStatus.Failed;
            tx.FailureReason = ex.Message;
            tx.CompletedAt = DateTime.UtcNow;
            Transactions.Update(tx);
            await Uow.SaveChangesAsync(ct);
            return Result<TransactionDto>.Failure("TRANSACTION_FAILED", tx.FailureReason);
        }

        var success = string.Equals(resp.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
        tx.Status = success ? TransactionStatus.Success : TransactionStatus.Failed;
        tx.ExternalRef = resp.ExternalRef;
        tx.FailureReason = success ? null : resp.FailureReason;
        tx.CompletedAt = DateTime.UtcNow;
        if (success && schema.IsBankManaged)
            tx.AccountingStatus = AccountingStatus.Delegated;
        Transactions.Update(tx);
        await Uow.SaveChangesAsync(ct);

        await Webhooks.EnqueueAsync(tx.PartnerId, tx.TransactionId,
            $"transaction.{(success ? "success" : "failed")}", new
            {
                tx.TransactionId,
                tx.PartnerTransactionRef,
                tx.Status,
                tx.Amount,
                tx.FeeAmount,
                tx.Currency,
                tx.ExternalRef,
            }, ct);

        if (success)
            return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));

        return Result<TransactionDto>.Failure("TRANSACTION_FAILED", tx.FailureReason ?? "Transaction failed");
    }
}
