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

public record WalletDebitCommand(Guid PartnerId, TransactionRequest Request) : IRequest<Result<TransactionDto>>;

public class WalletDebitValidator : AbstractValidator<WalletDebitCommand>
{
    public WalletDebitValidator()
    {
        RuleFor(x => x.Request.PartnerTransactionRef).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.SubscriptionId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
    }
}

public class WalletDebitCommandHandler : FinancialBaseHandler, IRequestHandler<WalletDebitCommand, Result<TransactionDto>>
{
    private readonly IWalletApiClient _wallet;

    public WalletDebitCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IUnitOfWork uow, IFeeCalculator feeCalculator, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletDebitCommandHandler> logger, IWalletApiClient wallet)
        : base(transactions, subscriptions, partners, uow, feeCalculator, accounting, webhooks, mapper, logger)
    {
        _wallet = wallet;
    }

    public async Task<Result<TransactionDto>> Handle(WalletDebitCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<TransactionDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var sub = await EnsureActiveSubscriptionAsync(request.Request.SubscriptionId, request.PartnerId, cancellationToken);
        if (sub is null) return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = await BuildTransactionAsync(request.Request, sub, request.PartnerId, TransactionType.WalletDebit, cancellationToken);
        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _wallet.DebitAsync(partner, new WalletTransactionRequest(
                tx.PartnerTransactionRef, sub.PhoneNumber, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

            await FinalizeAsync(tx, resp.ExternalRef,
                success: resp.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase),
                failureReason: resp.FailureReason, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Wallet debit failed for {TxId}", tx.TransactionId);
            await FinalizeAsync(tx, null, false, ex.Message, cancellationToken);
        }

        return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));
    }
}

public record WalletCreditCommand(Guid PartnerId, TransactionRequest Request) : IRequest<Result<TransactionDto>>;

public class WalletCreditValidator : AbstractValidator<WalletCreditCommand>
{
    public WalletCreditValidator()
    {
        RuleFor(x => x.Request.PartnerTransactionRef).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.SubscriptionId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
    }
}

public class WalletCreditCommandHandler : FinancialBaseHandler, IRequestHandler<WalletCreditCommand, Result<TransactionDto>>
{
    private readonly IWalletApiClient _wallet;

    public WalletCreditCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IUnitOfWork uow, IFeeCalculator feeCalculator, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletCreditCommandHandler> logger, IWalletApiClient wallet)
        : base(transactions, subscriptions, partners, uow, feeCalculator, accounting, webhooks, mapper, logger)
    {
        _wallet = wallet;
    }

    public async Task<Result<TransactionDto>> Handle(WalletCreditCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<TransactionDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var sub = await EnsureActiveSubscriptionAsync(request.Request.SubscriptionId, request.PartnerId, cancellationToken);
        if (sub is null) return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = await BuildTransactionAsync(request.Request, sub, request.PartnerId, TransactionType.WalletCredit, cancellationToken);
        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _wallet.CreditAsync(partner, new WalletTransactionRequest(
                tx.PartnerTransactionRef, sub.PhoneNumber, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

            await FinalizeAsync(tx, resp.ExternalRef,
                success: resp.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase),
                failureReason: resp.FailureReason, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Wallet credit failed for {TxId}", tx.TransactionId);
            await FinalizeAsync(tx, null, false, ex.Message, cancellationToken);
        }

        return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));
    }
}

public record WalletCancelCommand(Guid PartnerId, CancelTransactionRequest Request) : IRequest<Result<TransactionDto>>;

public class WalletCancelValidator : AbstractValidator<WalletCancelCommand>
{
    public WalletCancelValidator()
    {
        RuleFor(x => x.Request.PartnerTransactionRef).NotEmpty();
        RuleFor(x => x.Request.OriginalExternalRef).NotEmpty();
    }
}

public class WalletCancelCommandHandler : FinancialBaseHandler, IRequestHandler<WalletCancelCommand, Result<TransactionDto>>
{
    private readonly IWalletApiClient _wallet;

    public WalletCancelCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IUnitOfWork uow, IFeeCalculator feeCalculator, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletCancelCommandHandler> logger, IWalletApiClient wallet)
        : base(transactions, subscriptions, partners, uow, feeCalculator, accounting, webhooks, mapper, logger)
    {
        _wallet = wallet;
    }

    public async Task<Result<TransactionDto>> Handle(WalletCancelCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<TransactionDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        // Look up original transaction by external ref for context (mirror inverse logic in accounting engine)
        var original = (await Transactions.FindAsync(t => t.ExternalRef == request.Request.OriginalExternalRef && t.PartnerId == request.PartnerId, cancellationToken))
            .FirstOrDefault();
        if (original is null)
            return Result<TransactionDto>.Failure("ORIGINAL_NOT_FOUND", "Original transaction not found for the provided external reference.");

        var tx = await BuildTransactionAsync(new TransactionRequest(request.Request.PartnerTransactionRef, original.SubscriptionId,
            original.Amount, original.Currency, "Cancellation"),
            original.Subscription!, request.PartnerId, TransactionType.WalletCancel, cancellationToken);
        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _wallet.CancelAsync(partner, request.Request.OriginalExternalRef, cancellationToken);
            var success = resp.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
            if (success)
            {
                original.Status = TransactionStatus.Reversed;
                Transactions.Update(original);
            }
            await FinalizeAsync(tx, resp.ExternalRef, success, resp.FailureReason, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Wallet cancel failed for {TxId}", tx.TransactionId);
            await FinalizeAsync(tx, null, false, ex.Message, cancellationToken);
        }

        return Result<TransactionDto>.Success(Mapper.Map<TransactionDto>(tx));
    }
}
