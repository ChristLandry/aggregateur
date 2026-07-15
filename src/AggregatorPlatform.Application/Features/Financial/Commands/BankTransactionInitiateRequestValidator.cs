using AggregatorPlatform.Application.DTOs;
using FluentValidation;

namespace AggregatorPlatform.Application.Features.Financial.Commands;

/// <summary>
/// Validation du payload d'initiation bancaire (bank/debit + bank/credit).
/// </summary>
public sealed class BankTransactionInitiateRequestValidator : AbstractValidator<BankTransactionInitiateRequest>
{
    public BankTransactionInitiateRequestValidator()
    {
        RuleFor(x => x.PartnerTransactionRef)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BankAccount).NotEmpty().WithMessage("BankAccount is required.");
        RuleFor(x => x.BankAccount).MaximumLength(50);

        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumber is required.");
        RuleFor(x => x.PhoneNumber).MaximumLength(20);

        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.Currency).NotEmpty().Length(3);

        RuleFor(x => x.Fees!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Fees.HasValue)
            .WithMessage("Fees must be greater than or equal to 0 when provided.");

        RuleFor(x => x.Description).MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
