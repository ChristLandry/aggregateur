using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

public record PartnerDto(
    Guid PartnerId,
    string PartnerCode,
    string Name,
    string BaseUrl,
    string? AccountCode,
    PartnerStatus Status,
    string Currency,
    string? WebhookUrl,
    int RateLimitPerMin,
    bool RequireHmac,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreatePartnerRequest(
    string PartnerCode,
    string Name,
    string BaseUrl,
    string Currency,
    string? AccountCode,
    string? WebhookUrl,
    int RateLimitPerMin,
    string? IpWhitelist,
    bool RequireHmac);

public record CreatePartnerResponse(Guid PartnerId, string PartnerCode, string ApiKey);

public record UpdatePartnerRequest(
    string Name,
    string BaseUrl,
    string? AccountCode,
    string? WebhookUrl,
    int RateLimitPerMin,
    string? IpWhitelist,
    bool RequireHmac);

public record ChangePartnerStatusRequest(PartnerStatus Status);

public record PartnerAccountDto(
    Guid AccountId,
    Guid PartnerId,
    decimal Balance,
    string Currency,
    DateTime? LastMovementAt);

public record RotateApiKeyResponse(Guid PartnerId, string ApiKey);
