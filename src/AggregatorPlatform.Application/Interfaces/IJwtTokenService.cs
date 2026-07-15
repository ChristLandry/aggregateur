using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token);
}
