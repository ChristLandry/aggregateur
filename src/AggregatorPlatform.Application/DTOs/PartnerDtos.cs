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
    string? ContactEmail,
    string? ContactPhone,
    int? LowBalanceThresholdPercent,
    decimal? LowBalanceReferenceAmount,
    AlertChannels? AlertChannels,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreatePartnerRequest(
    string PartnerCode,
    string Name,
    string BaseUrl,
    string Currency,
    string? PartnerBankAccount,
    string? AccountCode,
    string? WebhookUrl,
    string? IpWhitelist,
    string? ContactEmail = null,
    string? ContactPhone = null,
    int? LowBalanceThresholdPercent = null,
    decimal? LowBalanceReferenceAmount = null,
    AlertChannels? AlertChannels = null);

public record CreatePartnerResponse(Guid PartnerId, string PartnerCode, string ApiKey);

/// <summary>
/// Payload PATCH partiel : seules les proprietes renseignees (non-null) sont
/// appliquees a l'entite. Une valeur omise dans le JSON reste a null et
/// la valeur existante en BD est preservee.
/// </summary>
public record UpdatePartnerRequest(
    string? Name,
    string? BaseUrl,
    string? AccountCode,
    string? WebhookUrl,
    string? IpWhitelist,
    string? Currency,
    string? PartnerBankAccount,
    string? ContactEmail = null,
    string? ContactPhone = null,
    int? LowBalanceThresholdPercent = null,
    decimal? LowBalanceReferenceAmount = null,
    AlertChannels? AlertChannels = null);

public record UpdatePartnerBalanceRequest(decimal Balance, string? Reason);

public record ChangePartnerStatusRequest(PartnerStatus Status);

public record PartnerAccountDto(
    Guid AccountId,
    Guid PartnerId,
    string PartnerBankAccount,
    decimal Balance,
    string Currency,
    DateTime? LastMovementAt);

public record RotateApiKeyResponse(Guid PartnerId, string ApiKey);

public record PartnerBalanceDto(Guid PartnerId, decimal Balance, string Currency, DateTime? LastMovementAt);
