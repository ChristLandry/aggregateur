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
    public WalletDebitValidator() => RuleFor(x => x.Request).SetValidator(new TransactionRequestValidator());
}

public class WalletDebitCommandHandler : FinancialBaseHandler, IRequestHandler<WalletDebitCommand, Result<TransactionDto>>
{
    private readonly IWalletConnectorResolver _walletResolver;

    public WalletDebitCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletDebitCommandHandler> logger, IWalletConnectorResolver walletResolver)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        _walletResolver = walletResolver;
    }

    public async Task<Result<TransactionDto>> Handle(WalletDebitCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.WalletDebit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var (sub, err) = await ResolveSubscriptionAsync(request.Request, request.PartnerId, cancellationToken);
        if (err == "SUBSCRIPTION_INVALID")
            return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = BuildTransaction(request.Request, sub, request.PartnerId, TransactionType.WalletDebit);

        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            // Appel HTTP au partenaire wallet => connecteur choisi par le resolver (PartnerCode).
            var resp = await _walletResolver.Resolve(partner!).DebitAsync(partner!, new WalletTransactionRequest(
                tx.PartnerTransactionRef, request.Request.PhoneNumber!, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

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
    public WalletCreditValidator() => RuleFor(x => x.Request).SetValidator(new TransactionRequestValidator());
}

public class WalletCreditCommandHandler : FinancialBaseHandler, IRequestHandler<WalletCreditCommand, Result<TransactionDto>>
{
    private readonly IWalletConnectorResolver _walletResolver;

    public WalletCreditCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletCreditCommandHandler> logger, IWalletConnectorResolver walletResolver)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        _walletResolver = walletResolver;
    }

    public async Task<Result<TransactionDto>> Handle(WalletCreditCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.WalletCredit, cancellationToken);
        if (preCheck is not null) return preCheck;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var (sub, err) = await ResolveSubscriptionAsync(request.Request, request.PartnerId, cancellationToken);
        if (err == "SUBSCRIPTION_INVALID")
            return Result<TransactionDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found, not active or not owned by partner.");

        var tx = BuildTransaction(request.Request, sub, request.PartnerId, TransactionType.WalletCredit);

        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            // Connecteur choisi via resolver a partir de PartnerCode.
            var resp = await _walletResolver.Resolve(partner!).CreditAsync(partner!, new WalletTransactionRequest(
                tx.PartnerTransactionRef, request.Request.PhoneNumber!, tx.Amount, tx.Currency, request.Request.Description), cancellationToken);

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
    private readonly IWalletConnectorResolver _walletResolver;

    public WalletCancelCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<WalletCancelCommandHandler> logger, IWalletConnectorResolver walletResolver)
        : base(transactions, subscriptions, partners, partnerEndpoints, uow, accounting, webhooks, mapper, logger)
    {
        _walletResolver = walletResolver;
    }

    public async Task<Result<TransactionDto>> Handle(WalletCancelCommand request, CancellationToken cancellationToken)
    {
        var idem = await CheckIdempotenceAsync(request.PartnerId, request.Request.PartnerTransactionRef, cancellationToken);
        if (idem is not null) return idem;

        // WalletCancel : pas de check PartnerEndpoint (derive de la transaction d'origine),
        // mais on garde la verification d'activite + ApiKey du partenaire.
        var preCheck = await PreValidatePartnerAsync(request.PartnerId, TransactionType.WalletCancel, cancellationToken);
        if (preCheck is not null) return preCheck;

        var partner = await Partners.GetByIdAsync(request.PartnerId, cancellationToken);

        var original = (await Transactions.FindAsync(t => t.ExternalRef == request.Request.OriginalExternalRef && t.PartnerId == request.PartnerId, cancellationToken))
            .FirstOrDefault();
        if (original is null)
            return Result<TransactionDto>.Failure("ORIGINAL_NOT_FOUND", "Original transaction not found for the provided external reference.");

        var cancelRequest = new TransactionRequest
        {
            PartnerTransactionRef = request.Request.PartnerTransactionRef,
            SubscriptionId = original.SubscriptionId,
            Amount = original.Amount,
            Fees = original.FeeAmount,
            Currency = original.Currency,
            Description = "Cancellation",
            BankAccount = original.BankAccount,
            PhoneNumber = original.PhoneNumber,
        };

        var tx = BuildTransaction(cancelRequest, original.Subscription, request.PartnerId, TransactionType.WalletCancel);
        await Transactions.AddAsync(tx, cancellationToken);
        await Uow.SaveChangesAsync(cancellationToken);

        try
        {
            var resp = await _walletResolver.Resolve(partner!).CancelAsync(partner!, request.Request.OriginalExternalRef, cancellationToken);
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
