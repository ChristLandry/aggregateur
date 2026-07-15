using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Subscriptions.Commands;
using AggregatorPlatform.Application.Features.Subscriptions.Queries;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/subscriptions")]
[RequirePartner]
public class SubscriptionController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;

    public SubscriptionController(ICurrentPartnerService currentPartner) => _currentPartner = currentPartner;

    /// <summary>
    /// Onboarding client complet : bank KYC + wallet KYC (via connector), comparaison
    /// (phone/dateOfBirth/nationalId), lien wallet-compte, puis creation Client + Customer + Subscription.
    /// </summary>
    [HttpPost("onboard")]
    public async Task<ActionResult<ApiResponse<OnboardCustomerResponse>>> Onboard([FromBody] OnboardCustomerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new OnboardCustomerCommand(request), ct));

    /// <summary>
    /// Cree une nouvelle souscription pour un client existant.
    /// Le partenaire est resolu UNIQUEMENT depuis le header X-Partner-ApiKey
    /// (middleware PartnerAuth). Aucun PartnerId n'est attendu dans le body.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateSubscriptionDirectRequest request, CancellationToken ct)
    {
        var currentPartnerId = _currentPartner.PartnerId!.Value;

        var subRequest = new CreateSubscriptionRequest(
            request.BankAccount,
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

    /// <summary>
    /// Liste les souscriptions.
    /// - Partenaire métier : limité à ce partenaire.
    /// - Partenaire WEB sans <c>partnerId</c> : toutes les souscriptions (vue admin).
    /// - Partenaire WEB avec <c>partnerId</c> : filtre sur ce partenaire.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionDto>>>> GetForPartner(
        [FromQuery] Guid? partnerId,
        [FromQuery] DateTime? subscribedAtDebut,
        [FromQuery] DateTime? subscribedAtFin,
        [FromQuery] string? phoneNumber,
        [FromQuery] string? bankAccount,
        [FromQuery] Guid? customerId,
        [FromQuery] string? phoneOperator,
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        Guid? resolvedPartnerId;
        if (partnerId.HasValue)
            resolvedPartnerId = partnerId;
        else if (_currentPartner.Current?.IsWebPartner == true)
            resolvedPartnerId = null; // toutes les souscriptions
        else
            resolvedPartnerId = _currentPartner.PartnerId!.Value;

        var q = new GetSubscriptionsByPartnerWithFilterQuery(
            resolvedPartnerId,
            subscribedAtDebut,
            subscribedAtFin,
            phoneNumber,
            bankAccount,
            customerId,
            phoneOperator,
            status ?? SubscriptionStatus.Active,
            take ?? 5000);

        return ToResponse(await Mediator.Send(q, ct));
    }

    /// <summary>Change le statut d'une souscription.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(Guid id, [FromBody] ChangeSubscriptionStatusRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new ChangeSubscriptionStatusCommand(id, request.Status), ct));
}
