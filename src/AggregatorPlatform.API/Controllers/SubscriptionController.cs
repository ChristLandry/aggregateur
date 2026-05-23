using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Subscriptions.Commands;
using AggregatorPlatform.Application.Features.Subscriptions.Queries;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/subscriptions")]
[RequirePartner]
public class SubscriptionController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;

    public SubscriptionController(ICurrentPartnerService currentPartner) => _currentPartner = currentPartner;

    /// <summary>
    /// Cree une nouvelle souscription pour un client existant et rattachee au partenaire courant.
    /// Le partenaire est resolu depuis le header X-Partner-Id (middleware PartnerAuth).
    /// Le PartnerId peut etre fourni dans le body pour rendre le lien explicite, mais il
    /// DOIT correspondre au partenaire authentifie sinon la requete est rejetee (403).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateSubscriptionDirectRequest request, CancellationToken ct)
    {
        var currentPartnerId = _currentPartner.PartnerId!.Value;

        // Validation explicite du lien Subscription <-> Partner.
        if (request.PartnerId.HasValue && request.PartnerId.Value != currentPartnerId)
        {
            return StatusCode(403, ApiResponse<Guid>.Fail(
                "PARTNER_MISMATCH",
                $"Le PartnerId fourni dans le body ({request.PartnerId}) ne correspond pas au partenaire authentifie ({currentPartnerId})."));
        }

        var subRequest = new CreateSubscriptionRequest(
            request.BankAccountNumber,
            request.BankCode,
            request.PhoneNumber,
            request.PhoneOperator,
            request.ExpiresAt);

        var result = await Mediator.Send(new CreateSubscriptionCommand(request.CustomerId, currentPartnerId, subRequest), ct);
        return ToResponse(result);
    }

    /// <summary>Recupere une souscription par son identifiant.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Get(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetSubscriptionByIdQuery(id), ct));

    /// <summary>Liste les souscriptions du partenaire courant (avec filtre optionnel par client).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionDto>>>> GetForPartner(
        [FromQuery] Guid? customerId, CancellationToken ct)
    {
        var partnerId = _currentPartner.PartnerId!.Value;
        return ToResponse(await Mediator.Send(new GetSubscriptionsByPartnerQuery(partnerId, customerId), ct));
    }

    /// <summary>Change le statut d'une souscription.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(Guid id, [FromBody] ChangeSubscriptionStatusRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new ChangeSubscriptionStatusCommand(id, request.Status), ct));
}
