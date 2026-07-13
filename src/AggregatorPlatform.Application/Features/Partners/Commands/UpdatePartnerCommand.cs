using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record UpdatePartnerCommand(Guid PartnerId, UpdatePartnerRequest Request) : IRequest<Result>;

/// <summary>
/// PATCH partiel : chaque regle ne s'applique que si la propriete est presente dans le payload.
/// </summary>
public class UpdatePartnerValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerValidator()
    {
        When(x => x.Request.Name is not null, () =>
            RuleFor(x => x.Request.Name!).NotEmpty().MaximumLength(200));

        When(x => x.Request.BaseUrl is not null, () =>
            RuleFor(x => x.Request.BaseUrl!)
                .NotEmpty()
                .Must(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                .WithMessage("BaseUrl must start with http:// or https://."));

        When(x => x.Request.AccountCode is not null, () =>
            RuleFor(x => x.Request.AccountCode!).MaximumLength(50));

        When(x => x.Request.WebhookUrl is not null, () =>
            RuleFor(x => x.Request.WebhookUrl!).MaximumLength(500));

        When(x => x.Request.IpWhitelist is not null, () =>
            RuleFor(x => x.Request.IpWhitelist!).MaximumLength(1000));

        When(x => x.Request.Currency is not null, () =>
            RuleFor(x => x.Request.Currency!).NotEmpty().Length(3));

        When(x => !string.IsNullOrEmpty(x.Request.PartnerBankAccount), () =>
            RuleFor(x => x.Request.PartnerBankAccount!).MaximumLength(64));

        When(x => !string.IsNullOrEmpty(x.Request.ContactEmail), () =>
            RuleFor(x => x.Request.ContactEmail!).EmailAddress().MaximumLength(200));

        When(x => !string.IsNullOrEmpty(x.Request.ContactPhone), () =>
            RuleFor(x => x.Request.ContactPhone!).MaximumLength(30));

        When(x => x.Request.LowBalanceThresholdPercent.HasValue, () =>
            RuleFor(x => x.Request.LowBalanceThresholdPercent!.Value).InclusiveBetween(1, 100));

        When(x => x.Request.LowBalanceReferenceAmount.HasValue, () =>
            RuleFor(x => x.Request.LowBalanceReferenceAmount!.Value).GreaterThan(0));
    }
}

public class UpdatePartnerCommandHandler : IRequestHandler<UpdatePartnerCommand, Result>
{
    private readonly IPartnerRepository _partners;
    private readonly IPartnerAccountRepository _accounts;
    private readonly IUnitOfWork _uow;

    public UpdatePartnerCommandHandler(
        IPartnerRepository partners,
        IPartnerAccountRepository accounts,
        IUnitOfWork uow)
    {
        _partners = partners;
        _accounts = accounts;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdatePartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var r = request.Request;

        // PATCH partiel sur l'entite Partner.
        if (r.Name              is not null) partner.Name            = r.Name;
        if (r.BaseUrl           is not null) partner.BaseUrl         = r.BaseUrl;
        if (r.AccountCode       is not null) partner.AccountCode     = r.AccountCode;
        if (r.WebhookUrl        is not null) partner.WebhookUrl      = r.WebhookUrl;
        if (r.IpWhitelist       is not null) partner.IpWhitelist     = r.IpWhitelist;
        if (r.Currency          is not null) partner.Currency        = r.Currency;
        if (r.ContactEmail      is not null) partner.ContactEmail    = r.ContactEmail;
        if (r.ContactPhone      is not null) partner.ContactPhone    = r.ContactPhone;
        if (r.LowBalanceThresholdPercent.HasValue) partner.LowBalanceThresholdPercent = r.LowBalanceThresholdPercent;
        if (r.LowBalanceReferenceAmount.HasValue)  partner.LowBalanceReferenceAmount  = r.LowBalanceReferenceAmount;
        if (r.AlertChannels.HasValue)              partner.AlertChannels              = r.AlertChannels;
        _partners.Update(partner);

        // PATCH partiel sur l'entite PartnerAccount (PartnerBankAccount, Currency).
        if (r.PartnerBankAccount is not null || r.Currency is not null)
        {
            var account = await _accounts.GetByPartnerIdAsync(request.PartnerId, cancellationToken);
            if (account is not null)
            {
                if (r.PartnerBankAccount is not null) account.PartnerBankAccount = r.PartnerBankAccount;
                if (r.Currency           is not null) account.Currency           = r.Currency;
                _accounts.Update(account);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
