using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.PartnerEndpoints;

public record GetPartnerEndpointByIdQuery(Guid PartnerEndpointId)
    : IRequest<Result<PartnerEndpointDto>>;

public record ListPartnerEndpointsQuery(Guid? PartnerId)
    : IRequest<Result<IReadOnlyList<PartnerEndpointDto>>>;

internal static class PartnerEndpointMapping
{
    public static PartnerEndpointDto ToDto(PartnerEndpoint e) => new(
        e.PartnerEndpointId, e.PartnerId, e.EndpointKey,
        e.SchemaId, e.Schema?.Name,
        e.CreatedAt, e.UpdatedAt);
}

public class GetPartnerEndpointByIdQueryHandler
    : IRequestHandler<GetPartnerEndpointByIdQuery, Result<PartnerEndpointDto>>
{
    private readonly IPartnerEndpointRepository _repo;
    public GetPartnerEndpointByIdQueryHandler(IPartnerEndpointRepository repo) => _repo = repo;

    public async Task<Result<PartnerEndpointDto>> Handle(GetPartnerEndpointByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repo.Query()
            .Include(e => e.Schema)
            .FirstOrDefaultAsync(e => e.PartnerEndpointId == request.PartnerEndpointId, cancellationToken);
        if (entity is null)
            return Result<PartnerEndpointDto>.Failure("PARTNER_ENDPOINT_NOT_FOUND", "Partner endpoint link not found.");
        return Result<PartnerEndpointDto>.Success(PartnerEndpointMapping.ToDto(entity));
    }
}

public class ListPartnerEndpointsQueryHandler
    : IRequestHandler<ListPartnerEndpointsQuery, Result<IReadOnlyList<PartnerEndpointDto>>>
{
    private readonly IPartnerEndpointRepository _repo;
    public ListPartnerEndpointsQueryHandler(IPartnerEndpointRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<PartnerEndpointDto>>> Handle(ListPartnerEndpointsQuery request, CancellationToken cancellationToken)
    {
        var q = _repo.Query().Include(e => e.Schema).AsQueryable();
        if (request.PartnerId.HasValue)
            q = q.Where(e => e.PartnerId == request.PartnerId.Value);
        var list = await q.OrderBy(e => e.PartnerId).ThenBy(e => e.EndpointKey).ToListAsync(cancellationToken);
        IReadOnlyList<PartnerEndpointDto> dtos = list.Select(PartnerEndpointMapping.ToDto).ToList();
        return Result<IReadOnlyList<PartnerEndpointDto>>.Success(dtos);
    }
}
