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

public record BankDebitCommand(Guid PartnerId, TransactionRequest Request) : IRequest<Result<TransactionDto>>;

public class BankDebitValidator : AbstractValidator<BankDebitCommand>
{
    public BankDebitValidator() => RuleFor(x => x.Request).SetValidator(new TransactionRequestValidator());
}

public class BankDebitCommandHandler : FinancialBaseHandler, IRequestHandler<BankDebitCommand, Result<TransactionDto>>
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
        // 1) Idempotence
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        // 2) Pre-validation : partenaire actif + ApiKey + PartnerEndpoint(BankDebit) + schema lie
        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.BankDebit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var (sub, err) = await ResolveSubscriptionAsync(request.Request, request.PartnerId, cancellationToken);
        if (err == "SUBSCRIPTION_INVALID")
            return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = BuildTransaction(request.Request, sub, request.PartnerId, TransactionType.BankDebit);

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

        return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));
    }
}

public record BankCreditCommand(Guid PartnerId, TransactionRequest Request) : IRequest<Result<TransactionDto>>;

public class BankCreditValidator : AbstractValidator<BankCreditCommand>
{
    public BankCreditValidator() => RuleFor(x => x.Request).SetValidator(new TransactionRequestValidator());
}

public class BankCreditCommandHandler : FinancialBaseHandler, IRequestHandler<BankCreditCommand, Result<TransactionDto>>
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
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.BankCredit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var (sub, err) = await ResolveSubscriptionAsync(request.Request, request.PartnerId, cancellationToken);
        if (err == "SUBSCRIPTION_INVALID")
            return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = BuildTransaction(request.Request, sub, request.PartnerId, TransactionType.BankCredit);

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

        return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));
    }
}
