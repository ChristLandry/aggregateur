using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record UpdatePartnerCommand(Guid PartnerId, UpdatePartnerRequest Request) : IRequest<Result>;

/// <summary>
/// Validation conditionnelle : chaque regle ne s'applique que si la propriete a ete renseignee.
/// Un champ omis dans le payload (null) ne declenche aucune validation et reste inchange en BD.
/// </summary>
public class UpdatePartnerValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerValidator()
    {
        When(x => x.Request.Name is not null, () =>
        {
            RuleFor(x => x.Request.Name!).NotEmpty().MaximumLength(200);
        });

        When(x => x.Request.BaseUrl is not null, () =>
        {
            RuleFor(x => x.Request.BaseUrl!)
                .NotEmpty()
                .Must(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                .WithMessage("BaseUrl must start with http:// or https://.");
        });

        When(x => x.Request.AccountCode is not null, () =>
        {
            RuleFor(x => x.Request.AccountCode!).MaximumLength(50);
        });

        When(x => x.Request.WebhookUrl is not null, () =>
        {
            RuleFor(x => x.Request.WebhookUrl!).MaximumLength(500);
        });

        When(x => x.Request.RateLimitPerMin.HasValue, () =>
        {
            RuleFor(x => x.Request.RateLimitPerMin!.Value).GreaterThan(0);
        });

        When(x => x.Request.IpWhitelist is not null, () =>
        {
            RuleFor(x => x.Request.IpWhitelist!).MaximumLength(1000);
        });
    }
}

public class UpdatePartnerCommandHandler : IRequestHandler<UpdatePartnerCommand, Result>
{
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _uow;

    public UpdatePartnerCommandHandler(IPartnerRepository partners, IUnitOfWork uow)
    {
        _partners = partners;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdatePartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var r = request.Request;

        // PATCH partiel : on n'affecte que les champs explicitement fournis.
        if (r.Name              is not null) partner.Name            = r.Name;
        if (r.BaseUrl           is not null) partner.BaseUrl         = r.BaseUrl;
        if (r.AccountCode       is not null) partner.AccountCode     = r.AccountCode;
        if (r.WebhookUrl        is not null) partner.WebhookUrl      = r.WebhookUrl;
        if (r.RateLimitPerMin.HasValue)      partner.RateLimitPerMin = r.RateLimitPerMin.Value;
        if (r.IpWhitelist       is not null) partner.IpWhitelist     = r.IpWhitelist;
        if (r.RequireHmac.HasValue)          partner.RequireHmac     = r.RequireHmac.Value;

        _partners.Update(partner);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
