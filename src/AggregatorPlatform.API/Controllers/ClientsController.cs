using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Clients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

/// <summary>
/// Gestion des Clients racines (identifies par BankAccountRoot).
/// Un Client peut porter plusieurs Customers (un par partenaire souscrit).
/// Reserve aux roles admin/finance (JWT), pas de header X-Partner-ApiKey.
/// </summary>
[Route("api/v1/clients")]
[Authorize(Roles = "Admin,SuperAdmin,Finance")]
public class ClientsController : BaseApiController
{
    /// <summary>Liste les Clients (limite au top <paramref name="take"/>, defaut 500).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientDto>>>> GetAll(
        [FromQuery] int? take,
        CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetAllClientsQuery(take), ct));

    /// <summary>Detail d'un Client avec ses Customers rattaches.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClientDetailDto>>> Get(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetClientByIdQuery(id), ct));
}
