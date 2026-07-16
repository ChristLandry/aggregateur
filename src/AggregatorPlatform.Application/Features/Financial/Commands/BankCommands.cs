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
    public BankDebitCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints, IAccountingSchemaRepository schemas,
        IBankApiClient bank, IRepository<Domain.Entities.Movement> movements,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<BankDebitCommandHandler> logger)
        : base(transactions, subscriptions, partners, partnerEndpoints, schemas, bank, movements,
               uow, accounting, webhooks, mapper, logger) { }

    public Task<Result<TransactionDto>> Handle(BankDebitCommand request, CancellationToken ct)
        => ProcessBankTransactionAsync(request.PartnerId, request.Request,
            TransactionType.BankDebit, FinancialEndpointKey.BankDebit, isDebit: true, ct);
}

public record BankCreditCommand(Guid PartnerId, BankTransactionInitiateRequest Request) : IRequest<Result<TransactionDto>>;

public class BankCreditValidator : AbstractValidator<BankCreditCommand>
{
    public BankCreditValidator() => RuleFor(x => x.Request).SetValidator(new BankTransactionInitiateRequestValidator());
}

public class BankCreditCommandHandler : BankBaseHandler, IRequestHandler<BankCreditCommand, Result<TransactionDto>>
{
    public BankCreditCommandHandler(
        ITransactionRepository transactions, ISubscriptionRepository subscriptions, IPartnerRepository partners,
        IPartnerEndpointRepository partnerEndpoints, IAccountingSchemaRepository schemas,
        IBankApiClient bank, IRepository<Domain.Entities.Movement> movements,
        IUnitOfWork uow, IAccountingEngine accounting, IWebhookService webhooks,
        IMapper mapper, ILogger<BankCreditCommandHandler> logger)
        : base(transactions, subscriptions, partners, partnerEndpoints, schemas, bank, movements,
               uow, accounting, webhooks, mapper, logger) { }

    public Task<Result<TransactionDto>> Handle(BankCreditCommand request, CancellationToken ct)
        => ProcessBankTransactionAsync(request.PartnerId, request.Request,
            TransactionType.BankCredit, FinancialEndpointKey.BankCredit, isDebit: false, ct);
}
