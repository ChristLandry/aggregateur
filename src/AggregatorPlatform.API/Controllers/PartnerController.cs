using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Partners.Commands;
using AggregatorPlatform.Application.Features.Partners.Queries;
using AggregatorPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/partners")]
[Authorize(Roles = "Admin,SuperAdmin,Finance")]
public class PartnerController : BaseApiController
{
    /// <summary>Create a new partner.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<CreatePartnerResponse>>> Create([FromBody] CreatePartnerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new CreatePartnerCommand(request), ct));

    /// <summary>
    /// Liste les codes partenaire autorises pour la creation (enum AllowedPartnerCode).
    /// Utile cote front pour alimenter la combobox PartnerCode et filtrer les codes deja crees.
    /// </summary>
    [HttpGet("allowed-codes")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<IReadOnlyList<string>>> GetAllowedCodes()
        => ToResponse(Application.Common.Result<IReadOnlyList<string>>.Success(
            Enum.GetNames<AllowedPartnerCode>()));

    /// <summary>List partners.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PartnerDto>>>> GetAll(CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetAllPartnersQuery(), ct));

    /// <summary>Get partner by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PartnerDto>>> GetById(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerByIdQuery(id), ct));

    /// <summary>Update a partner.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdatePartnerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new UpdatePartnerCommand(id, request), ct));

    /// <summary>Change partner status.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(Guid id, [FromBody] ChangePartnerStatusRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new ChangePartnerStatusCommand(id, request.Status), ct));

    /// <summary>Rotate API key.</summary>
    [HttpPost("{id:guid}/rotate-key")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<RotateApiKeyResponse>>> RotateKey(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new RotatePartnerApiKeyCommand(id), ct));

    /// <summary>Get partner mirror account (full DTO).</summary>
    [HttpGet("{id:guid}/account")]
    [Authorize(Roles = "Admin,SuperAdmin,Partner")]
    public async Task<ActionResult<ApiResponse<PartnerAccountDto>>> GetAccount(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerAccountQuery(id), ct));

    /// <summary>Get partner current balance (lightweight).</summary>
    [HttpGet("{id:guid}/balance")]
    [Authorize(Roles = "Admin,SuperAdmin,Partner,Finance")]
    public async Task<ActionResult<ApiResponse<PartnerBalanceDto>>> GetBalance(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerBalanceQuery(id), ct));

    /// <summary>Set partner balance (admin override). A PartnerAccountMovement is recorded for audit.</summary>
    [HttpPut("{id:guid}/balance")]
    [Authorize(Roles = "Admin,SuperAdmin,Finance")]
    public async Task<ActionResult<ApiResponse<PartnerBalanceDto>>> SetBalance(Guid id, [FromBody] UpdatePartnerBalanceRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new UpdatePartnerBalanceCommand(id, request), ct));
}
