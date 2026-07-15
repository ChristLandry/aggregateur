namespace AggregatorPlatform.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
