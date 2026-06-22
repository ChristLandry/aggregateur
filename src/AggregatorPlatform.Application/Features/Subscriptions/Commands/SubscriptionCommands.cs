using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Subscriptions.Commands;

public record CreateSubscriptionCommand(Guid CustomerId, Guid PartnerId, CreateSubscriptionRequest Request) : IRequest<Result<Guid>>;

public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.Request.BankAccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.PhoneOperator).NotEmpty().MaximumLength(50);
    }
}

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<Guid>>
{
    private readonly ISubscriptionRepository _subs;
    private readonly ICustomerRepository _customers;
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _uow;

    public CreateSubscriptionCommandHandler(ISubscriptionRepository subs, ICustomerRepository customers,
        IPartnerRepository partners, IUnitOfWork uow)
    {
        _subs = subs;
        _customers = customers;
        _partners = partners;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null) return Result<Guid>.Failure("CUSTOMER_NOT_FOUND", "Customer not found.");
        if (customer.Status != CustomerStatus.Active)
            return Result<Guid>.Failure("CUSTOMER_INACTIVE", "Customer is not active.");

        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<Guid>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        // Regle metier : une souscription est unique par le triplet exact
        // (PartnerId, BankAccountNumber, PhoneNumber). Verification applicative AVANT le
        // SaveChanges pour un code d'erreur explicite ; l'index unique compose en BD
        // garantit la regle meme en cas de race condition.
        var duplicate = await _subs.ExistsByPartnerBankAndPhoneAsync(
            request.PartnerId,
            request.Request.BankAccountNumber,
            request.Request.PhoneNumber,
            cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure("SUBSCRIPTION_DUPLICATE",
                "An active subscription already exists for this partner with the same bank account and phone number combination.");

        var sub = new Subscription
        {
            CustomerId = request.CustomerId,
            PartnerId = request.PartnerId,
            BankAccountNumber = request.Request.BankAccountNumber,
            PhoneNumber = request.Request.PhoneNumber,
            PhoneOperator = request.Request.PhoneOperator,
            ExpiresAt = request.Request.ExpiresAt
        };
        await _subs.AddAsync(sub, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(sub.SubscriptionId);
    }
}

public record ChangeSubscriptionStatusCommand(Guid SubscriptionId, SubscriptionStatus Status) : IRequest<Result>;

public class ChangeSubscriptionStatusCommandHandler : IRequestHandler<ChangeSubscriptionStatusCommand, Result>
{
    private readonly ISubscriptionRepository _subs;
    private readonly IUnitOfWork _uow;

    public ChangeSubscriptionStatusCommandHandler(ISubscriptionRepository subs, IUnitOfWork uow)
    {
        _subs = subs;
        _uow = uow;
    }

    public async Task<Result> Handle(ChangeSubscriptionStatusCommand request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null) return Result.Failure("SUBSCRIPTION_NOT_FOUND", "Subscription not found.");
        sub.Status = request.Status;
        _subs.Update(sub);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
