using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    /// <summary>Login with username/password (optionally with 2FA code).</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new LoginCommand(request), ct));

    /// <summary>Refresh access token using a refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new RefreshTokenCommand(request), ct));

    /// <summary>Logout (revoke refresh token).</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new LogoutCommand(request.RefreshToken), ct));
}
