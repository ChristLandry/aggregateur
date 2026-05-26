using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Customers.Commands;

public record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<Result<Guid>>;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Request.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("Customer must be at least 18 years old.");
        RuleFor(x => x.Request.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Request.Email));
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public CreateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork uow)
    {
        _customers = customers;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Request.ExternalCustomerId))
        {
            var existing = await _customers.GetByExternalIdAsync(request.Request.ExternalCustomerId, cancellationToken);
            if (existing is not null)
                return Result<Guid>.Failure("CUSTOMER_EXISTS", "Customer with this external id already exists.");
        }

        var customer = new Customer
        {
            ExternalCustomerId = request.Request.ExternalCustomerId,
            FullName = request.Request.FullName,
            DateOfBirth = request.Request.DateOfBirth,
            NationalId = request.Request.NationalId,
            Email = request.Request.Email
        };
        await _customers.AddAsync(customer, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(customer.CustomerId);
    }
}

public record UpdateCustomerCommand(Guid CustomerId, UpdateCustomerRequest Request) : IRequest<Result>;

/// <summary>
/// Validation conditionnelle : chaque regle ne s'applique que si la propriete est presente.
/// </summary>
public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        When(x => x.Request.FullName is not null, () =>
        {
            RuleFor(x => x.Request.FullName!).NotEmpty().MaximumLength(300);
        });

        When(x => !string.IsNullOrEmpty(x.Request.Email), () =>
        {
            RuleFor(x => x.Request.Email!).EmailAddress();
        });
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public UpdateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork uow)
    {
        _customers = customers;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null) return Result.Failure("CUSTOMER_NOT_FOUND", "Customer not found.");

        var r = request.Request;

        // PATCH partiel : seuls les champs explicitement fournis sont appliques.
        if (r.FullName     is not null) customer.FullName    = r.FullName;
        if (r.DateOfBirth.HasValue)     customer.DateOfBirth = r.DateOfBirth.Value;
        if (r.Email        is not null) customer.Email       = r.Email;
        if (r.Status.HasValue)          customer.Status      = r.Status.Value;
        if (r.KycStatus.HasValue)       customer.KycStatus   = r.KycStatus.Value;

        _customers.Update(customer);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
