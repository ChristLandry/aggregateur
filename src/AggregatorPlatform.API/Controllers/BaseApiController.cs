using AggregatorPlatform.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected ActionResult<ApiResponse<T>> ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<T>.Ok(result.Value!));
        return BadRequest(ApiResponse<T>.Fail(result.ErrorCode!, result.ErrorMessage!));
    }

    protected ActionResult<ApiResponse> ToResponse(Result result)
    {
        if (result.IsSuccess) return Ok(ApiResponse.Ok());
        return BadRequest(ApiResponse.Fail(result.ErrorCode!, result.ErrorMessage!));
    }
}
