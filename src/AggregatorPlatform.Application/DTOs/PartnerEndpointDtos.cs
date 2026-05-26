using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

/// <summary>Lien partenaire <-> endpoint financier (+ schema comptable optionnel).</summary>
public record PartnerEndpointDto(
    Guid PartnerEndpointId,
    Guid PartnerId,
    FinancialEndpointKey EndpointKey,
    Guid? SchemaId,
    string? SchemaName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Payload de creation du lien Partner <-> Endpoint. SchemaId optionnel.</summary>
public record CreatePartnerEndpointRequest(
    Guid PartnerId,
    FinancialEndpointKey EndpointKey,
    Guid? SchemaId);

/// <summary>Payload d'attachement d'un schema a un lien existant.</summary>
public record AttachSchemaRequest(Guid SchemaId);
