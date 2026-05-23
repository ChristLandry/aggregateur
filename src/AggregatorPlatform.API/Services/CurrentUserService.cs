using System.Security.Claims;
using AggregatorPlatform.Application.Interfaces;

namespace AggregatorPlatform.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var v = Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.Identity?.Name;
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
