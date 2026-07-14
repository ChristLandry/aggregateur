using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record RotatePartnerApiKeyCommand(Guid PartnerId) : IRequest<Result<RotateApiKeyResponse>>;

public class RotatePartnerApiKeyCommandHandler : IRequestHandler<RotatePartnerApiKeyCommand, Result<RotateApiKeyResponse>>
{
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _uow;
    private readonly IEncryptionService _encryption;
    private readonly ICacheService _cache;

    public RotatePartnerApiKeyCommandHandler(IPartnerRepository partners, IUnitOfWork uow,
        IEncryptionService encryption, ICacheService cache)
    {
        _partners = partners;
        _uow = uow;
        _encryption = encryption;
        _cache = cache;
    }

    public async Task<Result<RotateApiKeyResponse>> Handle(RotatePartnerApiKeyCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<RotateApiKeyResponse>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var apiKey = _encryption.GenerateApiKey();
        partner.ApiKey = _encryption.ComputeSha256(apiKey);
        partner.ApiKeyPlaintext = apiKey;   // met a jour aussi la version clair chiffree
        _partners.Update(partner);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.DeleteAsync($"partner:{partner.PartnerId}", cancellationToken);

        return Result<RotateApiKeyResponse>.Success(new RotateApiKeyResponse(partner.PartnerId, apiKey));
    }
}
