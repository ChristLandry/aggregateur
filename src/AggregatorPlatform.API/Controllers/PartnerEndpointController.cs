using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.PartnerEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

/// <summary>
/// Gestion des liaisons Partenaire/Endpoint financier (et schema comptable optionnel).
/// Modele : une ligne PartnerEndpoint represente l'eligibilite d'un partenaire pour
/// un endpoint donne (BankDebit / BankCredit / WalletDebit / WalletCredit).
/// Le SchemaId optionnel attache un schema comptable specifique a cette liaison ;
/// il peut etre attache/detache independamment du lien partenaire-endpoint.
/// </summary>
[Route("api/v1/partner-endpoints")]
[Authorize(Roles = "Admin,SuperAdmin,Finance")]
public class PartnerEndpointController : BaseApiController
{
    /// <summary>Liste les liaisons (filtre optionnel par partenaire).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PartnerEndpointDto>>>> List(
        [FromQuery] Guid? partnerId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new ListPartnerEndpointsQuery(partnerId), ct));

    /// <summary>Detail d'une liaison.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PartnerEndpointDto>>> GetById(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerEndpointByIdQuery(id), ct));

    /// <summary>Cree une liaison Partner-Endpoint (avec schema optionnel).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreatePartnerEndpointRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new CreatePartnerEndpointCommand(request), ct));

    /// <summary>Supprime la liaison Partner-Endpoint (et detache le schema cote).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new DeletePartnerEndpointCommand(id), ct));

    /// <summary>Attache (ou remplace) le schema comptable de cette liaison.</summary>
    [HttpPut("{id:guid}/schema")]
    public async Task<ActionResult<ApiResponse>> AttachSchema(
        Guid id, [FromBody] AttachSchemaRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new AttachSchemaCommand(id, request), ct));

    /// <summary>Detache le schema comptable (sans supprimer la liaison Partner-Endpoint).</summary>
    [HttpDelete("{id:guid}/schema")]
    public async Task<ActionResult<ApiResponse>> DetachSchema(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new DetachSchemaCommand(id), ct));
}
