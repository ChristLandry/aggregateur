using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record ChangePartnerStatusCommand(Guid PartnerId, PartnerStatus Status) : IRequest<Result>;

public class ChangePartnerStatusCommandHandler : IRequestHandler<ChangePartnerStatusCommand, Result>
{
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _uow;

    public ChangePartnerStatusCommandHandler(IPartnerRepository partners, IUnitOfWork uow)
    {
        _partners = partners;
        _uow = uow;
    }

    public async Task<Result> Handle(ChangePartnerStatusCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result.Failure("PARTNER_NOT_FOUND", "Partner not found.");
        partner.Status = request.Status;
        _partners.Update(partner);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
