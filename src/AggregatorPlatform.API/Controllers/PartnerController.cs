using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Partners.Commands;
using AggregatorPlatform.Application.Features.Partners.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/partners")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PartnerController : BaseApiController
{
    /// <summary>Create a new partner.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreatePartnerResponse>>> Create([FromBody] CreatePartnerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new CreatePartnerCommand(request), ct));

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
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdatePartnerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new UpdatePartnerCommand(id, request), ct));

    /// <summary>Change partner status.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(Guid id, [FromBody] ChangePartnerStatusRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new ChangePartnerStatusCommand(id, request.Status), ct));

    /// <summary>Rotate API key.</summary>
    [HttpPost("{id:guid}/rotate-key")]
    public async Task<ActionResult<ApiResponse<RotateApiKeyResponse>>> RotateKey(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new RotatePartnerApiKeyCommand(id), ct));

    /// <summary>Get partner mirror account.</summary>
    [HttpGet("{id:guid}/account")]
    [Authorize(Roles = "Admin,SuperAdmin,Partner")]
    public async Task<ActionResult<ApiResponse<PartnerAccountDto>>> GetAccount(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerAccountQuery(id), ct));
}
