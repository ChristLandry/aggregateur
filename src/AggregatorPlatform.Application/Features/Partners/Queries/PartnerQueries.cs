using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Queries;

public record GetPartnerByIdQuery(Guid PartnerId) : IRequest<Result<PartnerDto>>;
public record GetAllPartnersQuery() : IRequest<Result<IReadOnlyList<PartnerDto>>>;
public record GetPartnerAccountQuery(Guid PartnerId) : IRequest<Result<PartnerAccountDto>>;

/// <summary>Renvoie uniquement le solde courant + devise.</summary>
public record GetPartnerBalanceQuery(Guid PartnerId) : IRequest<Result<PartnerBalanceDto>>;

/// <summary>
/// Recupere la cle API en clair d'un partenaire. Endpoint sensible reserve
/// aux roles Admin/SuperAdmin. Renvoie <c>API_KEY_UNAVAILABLE</c> si la valeur
/// n'a pas ete persistee (cas des partenaires crees avant la migration
/// AddPartnerApiKeyPlaintext) : dans ce cas, seule une rotation regenere la valeur.
/// </summary>
public record GetPartnerApiKeyQuery(Guid PartnerId) : IRequest<Result<RotateApiKeyResponse>>;

public class GetPartnerApiKeyQueryHandler : IRequestHandler<GetPartnerApiKeyQuery, Result<RotateApiKeyResponse>>
{
    private readonly IPartnerRepository _partners;

    public GetPartnerApiKeyQueryHandler(IPartnerRepository partners) => _partners = partners;

    public async Task<Result<RotateApiKeyResponse>> Handle(GetPartnerApiKeyQuery request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null)
            return Result<RotateApiKeyResponse>.Failure("PARTNER_NOT_FOUND", "Partner not found.");
        if (string.IsNullOrWhiteSpace(partner.ApiKeyPlaintext))
            return Result<RotateApiKeyResponse>.Failure("API_KEY_UNAVAILABLE",
                "La cle en clair n'est pas disponible pour ce partenaire (cree avant la migration). Faire un rotate-key pour en regenerer une.");
        return Result<RotateApiKeyResponse>.Success(new RotateApiKeyResponse(partner.PartnerId, partner.ApiKeyPlaintext));
    }
}

public class GetPartnerByIdQueryHandler : IRequestHandler<GetPartnerByIdQuery, Result<PartnerDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly IMapper _mapper;

    public GetPartnerByIdQueryHandler(IPartnerRepository partners, IMapper mapper)
    {
        _partners = partners;
        _mapper = mapper;
    }

    public async Task<Result<PartnerDto>> Handle(GetPartnerByIdQuery request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<PartnerDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");
        return Result<PartnerDto>.Success(_mapper.Map<PartnerDto>(partner));
    }
}

public class GetAllPartnersQueryHandler : IRequestHandler<GetAllPartnersQuery, Result<IReadOnlyList<PartnerDto>>>
{
    private readonly IPartnerRepository _partners;
    private readonly IMapper _mapper;

    public GetAllPartnersQueryHandler(IPartnerRepository partners, IMapper mapper)
    {
        _partners = partners;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<PartnerDto>>> Handle(GetAllPartnersQuery request, CancellationToken cancellationToken)
    {
        // Les partenaires techniques (IsWebPartner = true) ne sont jamais exposes
        // dans la liste publique. Ils restent accessibles via GET /:id si l'admin
        // connait leur identifiant.
        var list = await _partners.GetAllAsync(cancellationToken);
        var filtered = list.Where(p => !p.IsWebPartner).ToList();
        return Result<IReadOnlyList<PartnerDto>>.Success(_mapper.Map<IReadOnlyList<PartnerDto>>(filtered));
    }
}

public class GetPartnerAccountQueryHandler : IRequestHandler<GetPartnerAccountQuery, Result<PartnerAccountDto>>
{
    private readonly IPartnerAccountRepository _accounts;
    private readonly IMapper _mapper;

    public GetPartnerAccountQueryHandler(IPartnerAccountRepository accounts, IMapper mapper)
    {
        _accounts = accounts;
        _mapper = mapper;
    }

    public async Task<Result<PartnerAccountDto>> Handle(GetPartnerAccountQuery request, CancellationToken cancellationToken)
    {
        var acc = await _accounts.GetByPartnerIdAsync(request.PartnerId, cancellationToken);
        if (acc is null) return Result<PartnerAccountDto>.Failure("ACCOUNT_NOT_FOUND", "Account not found.");
        return Result<PartnerAccountDto>.Success(_mapper.Map<PartnerAccountDto>(acc));
    }
}

public class GetPartnerBalanceQueryHandler : IRequestHandler<GetPartnerBalanceQuery, Result<PartnerBalanceDto>>
{
    private readonly IPartnerAccountRepository _accounts;

    public GetPartnerBalanceQueryHandler(IPartnerAccountRepository accounts) => _accounts = accounts;

    public async Task<Result<PartnerBalanceDto>> Handle(GetPartnerBalanceQuery request, CancellationToken cancellationToken)
    {
        var acc = await _accounts.GetByPartnerIdAsync(request.PartnerId, cancellationToken);
        if (acc is null) return Result<PartnerBalanceDto>.Failure("ACCOUNT_NOT_FOUND", "Partner account not found.");
        return Result<PartnerBalanceDto>.Success(new PartnerBalanceDto(
            acc.PartnerId, acc.Balance, acc.Currency, acc.LastMovementAt));
    }
}
