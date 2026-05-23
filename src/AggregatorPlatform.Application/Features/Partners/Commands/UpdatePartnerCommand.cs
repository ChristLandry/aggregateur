using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record UpdatePartnerCommand(Guid PartnerId, UpdatePartnerRequest Request) : IRequest<Result>;

public class UpdatePartnerValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.BaseUrl).NotEmpty()
            .Must(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Request.RateLimitPerMin).GreaterThan(0);
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

        partner.Name = request.Request.Name;
        partner.BaseUrl = request.Request.BaseUrl;
        partner.AccountCode = request.Request.AccountCode;
        partner.WebhookUrl = request.Request.WebhookUrl;
        partner.RateLimitPerMin = request.Request.RateLimitPerMin;
        partner.IpWhitelist = request.Request.IpWhitelist;
        partner.RequireHmac = request.Request.RequireHmac;

        _partners.Update(partner);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
