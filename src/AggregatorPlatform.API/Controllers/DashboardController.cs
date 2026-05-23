using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : BaseApiController
{
    /// <summary>Admin dashboard summary.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<AdminDashboardSummaryDto>>> AdminSummary(CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetDashboardSummaryQuery(), ct));

    /// <summary>Partner dashboard summary.</summary>
    [HttpGet("partners/{id:guid}/summary")]
    public async Task<ActionResult<ApiResponse<PartnerDashboardSummaryDto>>> PartnerSummary(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerDashboardQuery(id), ct));
}
