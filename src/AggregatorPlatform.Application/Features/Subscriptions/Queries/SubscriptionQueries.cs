using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using AggregatorPlatform.Domain.Enums;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Subscriptions.Queries;

public record GetSubscriptionByIdQuery(Guid SubscriptionId) : IRequest<Result<SubscriptionDto>>;

public record GetSubscriptionsByPartnerQuery(Guid PartnerId, Guid? CustomerId) : IRequest<Result<IReadOnlyList<SubscriptionDto>>>;

public record GetSubscriptionsByPartnerWithFilterQuery(
    Guid PartnerId,
    DateTime? SubscribedAtDebut,
    DateTime? SubscribedAtFin,
    string? PhoneNumber,
    string? BankAccountNumber,
    Guid? CustomerId,
    string? PhoneOperator,
    SubscriptionStatus Status,
    int Take) : IRequest<Result<IReadOnlyList<SubscriptionDto>>>;

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

public class GetSubscriptionsByPartnerWithFilterQueryHandler : IRequestHandler<GetSubscriptionsByPartnerWithFilterQuery, Result<IReadOnlyList<SubscriptionDto>>>
{
    private readonly ISubscriptionRepository _subs;
    private readonly IMapper _mapper;

    public GetSubscriptionsByPartnerWithFilterQueryHandler(ISubscriptionRepository subs, IMapper mapper)
    {
        _subs = subs;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> Handle(GetSubscriptionsByPartnerWithFilterQuery request, CancellationToken cancellationToken)
    {
        var query = _subs.Query().Where(s => s.PartnerId == request.PartnerId);

        if (request.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);

        // PhoneNumber et BankAccountNumber sont chiffres AES-256 (deterministe)
        // au repos. Seule l'EGALITE EXACTE est traduisible : un LIKE %x% s'appliquerait
        // au ciphertext et serait inutile. On force donc une comparaison stricte ;
        // le converter EF se charge de chiffrer la valeur de recherche pour matcher
        // le ciphertext stocke.
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            query = query.Where(s => s.PhoneNumber == request.PhoneNumber);

        if (!string.IsNullOrWhiteSpace(request.BankAccountNumber))
            query = query.Where(s => s.BankAccountNumber == request.BankAccountNumber);

        if (!string.IsNullOrWhiteSpace(request.PhoneOperator))
            query = query.Where(s => s.PhoneOperator == request.PhoneOperator);

        if (request.SubscribedAtDebut.HasValue)
            query = query.Where(s => s.SubscribedAt >= request.SubscribedAtDebut.Value);

        if (request.SubscribedAtFin.HasValue)
            query = query.Where(s => s.SubscribedAt <= request.SubscribedAtFin.Value);

        // Status filter (default provided by controller)
        query = query.Where(s => s.Status == request.Status);

        var list = await query.OrderByDescending(s => s.SubscribedAt).Take(request.Take).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<SubscriptionDto>>.Success(_mapper.Map<IReadOnlyList<SubscriptionDto>>(list));
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
