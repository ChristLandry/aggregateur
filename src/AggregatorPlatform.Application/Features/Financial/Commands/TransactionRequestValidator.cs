using AggregatorPlatform.Application.DTOs;
using FluentValidation;

namespace AggregatorPlatform.Application.Features.Financial.Commands;

/// <summary>
/// Regles de validation communes a tous les payloads financiers (debit/credit, bank/wallet).
/// </summary>
public sealed class TransactionRequestValidator : AbstractValidator<TransactionRequest>
{
    public TransactionRequestValidator()
    {
        RuleFor(x => x.PartnerTransactionRef)
            .NotEmpty()
            .MaximumLength(100);

        // BankAccount et PhoneNumber sont desormais OBLIGATOIRES.
        RuleFor(x => x.BankAccount)
            .NotEmpty()
            .MaximumLength(11)
            .WithMessage("BankAccount is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(13)
            .WithMessage("PhoneNumber is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        // Frais : optionnels, mais si fournis doivent etre >= 0
        RuleFor(x => x.Fees!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Fees.HasValue)
            .WithMessage("Fees must be greater than or equal to 0 when provided.");

        // SubscriptionId reste optionnel (traçabilite uniquement).
        RuleFor(x => x.Description).MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
