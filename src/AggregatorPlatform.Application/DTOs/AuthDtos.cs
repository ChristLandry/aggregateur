namespace AggregatorPlatform.Application.DTOs;

public record LoginRequest(string Username, string Password, string? TwoFactorCode);

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string Role);

public record RefreshTokenRequest(string RefreshToken);
