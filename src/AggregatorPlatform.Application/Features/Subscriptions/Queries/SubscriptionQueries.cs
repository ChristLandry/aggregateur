using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Subscriptions.Queries;

public record GetSubscriptionByIdQuery(Guid SubscriptionId) : IRequest<Result<SubscriptionDto>>;

public record GetSubscriptionsByPartnerQuery(Guid PartnerId, Guid? CustomerId) : IRequest<Result<IReadOnlyList<SubscriptionDto>>>;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subs;
    private readonly IMapper _mapper;

    public GetSubscriptionByIdQueryHandler(ISubscriptionRepository subs, IMapper mapper)
    {
        _subs = subs;
        _mapper = mapper;
    }

    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null) return Result<SubscriptionDto>.Failure("SUBSCRIPTION_NOT_FOUND", "Subscription not found.");
        return Result<SubscriptionDto>.Success(_mapper.Map<SubscriptionDto>(sub));
    }
}

public class GetSubscriptionsByPartnerQueryHandler : IRequestHandler<GetSubscriptionsByPartnerQuery, Result<IReadOnlyList<SubscriptionDto>>>
{
    private readonly ISubscriptionRepository _subs;
    private readonly IMapper _mapper;

    public GetSubscriptionsByPartnerQueryHandler(ISubscriptionRepository subs, IMapper mapper)
    {
        _subs = subs;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> Handle(GetSubscriptionsByPartnerQuery request, CancellationToken cancellationToken)
    {
        var query = _subs.Query().Where(s => s.PartnerId == request.PartnerId);
        if (request.CustomerId.HasValue) query = query.Where(s => s.CustomerId == request.CustomerId.Value);

        var list = await query.OrderByDescending(s => s.SubscribedAt).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<SubscriptionDto>>.Success(_mapper.Map<IReadOnlyList<SubscriptionDto>>(list));
    }
}
