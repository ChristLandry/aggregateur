using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Application.Features.Financial.Commands;

public record BankDebitCommand(Guid PartnerId, BankTransactionInitiateRequest Request) : IRequest<Result<TransactionDto>>;

public class BankDebitValidator : AbstractValidator<BankDebitCommand>
{
    public BankDebitValidator()
    {
        RuleFor(x => x.Request).SetValidator(new BankTransactionInitiateRequestValidator());
        RuleFor(x => x.Request.OperationType)
            .NotEmpty().WithMessage("OperationType est obligatoire pour /api/v1/bank/debit.")
            .Must(op => op == "BTW" || op == "WTB")
            .WithMessage("OperationType doit etre 'BTW' (debit vers wallet) ou 'WTB' (credit depuis wallet).");
    }
}

public class BankDebitCommandHandler : BankBaseHandler, IRequestHandler<BankDebitCommand, Result<TransactionDto>>
{
    private readonly IBankApiClient _bank;

    public BankDebitCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<BankDebitCommandHandler> logger, IBankApiClient bank)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        _bank = bank;
    }

    public async Task<Result<TransactionDto>> Handle(BankDebitCommand request, CancellationToken cancellationToken)
    {
        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.BankDebit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var dup = await EnsureNoDuplicatePartnerRefAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (dup is not null) return dup;

        var (sub, subErr) = await ResolveBankSubscriptionOrFailAsync(
            request.PartnerId, request.Request.PhoneNumber, request.Request.BankAccount, cancellationToken);
        if (subErr is not null) return subErr;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var tx = BuildBankTransaction(request.Request, sub!, request.PartnerId, TransactionType.BankDebit);

        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _bank.DebitAsync(partner!, new BankTransactionRequest(
                tx.PartnerTransactionRef, request.Request.BankAccount!, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

            await FinalizeAsync(tx, resp.ExternalRef,
                success: resp.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase),
                failureReason: resp.FailureReason, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Bank debit failed for {TxId}", tx.TransactionId);
            await FinalizeAsync(tx, null, false, ex.Message, cancellationToken);
        }

        if (tx.Status == TransactionStatus.Success)
            return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));

        return Result<TransactionDto>.Failure("TRANSACTION_FAILED", tx.FailureReason ?? "Transaction failed");
    }
}

public record BankCreditCommand(Guid PartnerId, BankTransactionInitiateRequest Request) : IRequest<Result<TransactionDto>>;

public class BankCreditValidator : AbstractValidator<BankCreditCommand>
{
    public BankCreditValidator() => RuleFor(x => x.Request).SetValidator(new BankTransactionInitiateRequestValidator());
}

public class BankCreditCommandHandler : BankBaseHandler, IRequestHandler<BankCreditCommand, Result<TransactionDto>>
{
    private readonly IBankApiClient _bank;

    public BankCreditCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<BankCreditCommandHandler> logger, IBankApiClient bank)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        _bank = bank;
    }

    public async Task<Result<TransactionDto>> Handle(BankCreditCommand request, CancellationToken cancellationToken)
    {
        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.BankCredit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var dup = await EnsureNoDuplicatePartnerRefAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (dup is not null) return dup;

        var (sub, subErr) = await ResolveBankSubscriptionOrFailAsync(
            request.PartnerId, request.Request.PhoneNumber, request.Request.BankAccount, cancellationToken);
        if (subErr is not null) return subErr;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var tx = BuildBankTransaction(request.Request, sub!, request.PartnerId, TransactionType.BankCredit);

        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _bank.CreditAsync(partner!, new BankTransactionRequest(
                tx.PartnerTransactionRef, request.Request.BankAccount!, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

            await FinalizeAsync(tx, resp.ExternalRef,
                success: resp.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase),
                failureReason: resp.FailureReason, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Bank credit failed for {TxId}", tx.TransactionId);
            await FinalizeAsync(tx, null, false, ex.Message, cancellationToken);
        }

        if (tx.Status == TransactionStatus.Success)
            return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));

        return Result<TransactionDto>.Failure("TRANSACTION_FAILED", tx.FailureReason ?? "Transaction failed");
    }
}
