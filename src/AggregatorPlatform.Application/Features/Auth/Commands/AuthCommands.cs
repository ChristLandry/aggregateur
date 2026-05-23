using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<Result<LoginResponse>>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Request.Username).NotEmpty();
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;
    private readonly ITwoFactorService _twoFactor;

    public LoginCommandHandler(IUserRepository users, IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow, IJwtTokenService jwt, ITwoFactorService twoFactor)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _jwt = jwt;
        _twoFactor = twoFactor;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByUsernameAsync(request.Request.Username, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("INVALID_CREDENTIALS", "Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("INVALID_CREDENTIALS", "Invalid credentials.");

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.Request.TwoFactorCode))
                return Result<LoginResponse>.Failure("TWO_FACTOR_REQUIRED", "Two-factor authentication code required.");
            if (!_twoFactor.ValidateCode(user.TwoFactorSecret!, request.Request.TwoFactorCode))
                return Result<LoginResponse>.Failure("INVALID_2FA", "Invalid 2FA code.");
        }

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenStr = _jwt.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _refreshTokens.AddAsync(refreshToken, cancellationToken);
        user.LastLoginAt = DateTime.UtcNow;
        _users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(accessToken, refreshTokenStr,
            DateTime.UtcNow.AddMinutes(60), user.Role.ToString()));
    }
}

public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<Result<LoginResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IUserRepository users, IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow, IJwtTokenService jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _jwt = jwt;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokens.GetByTokenAsync(request.Request.RefreshToken, cancellationToken);
        if (existing is null || !existing.IsActive)
            return Result<LoginResponse>.Failure("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");

        var user = await _users.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("USER_INACTIVE", "User account is inactive.");

        var newAccess = _jwt.GenerateAccessToken(user);
        var newRefresh = _jwt.GenerateRefreshToken();
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefresh;
        _refreshTokens.Update(existing);
        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.UserId,
            Token = newRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(newAccess, newRefresh,
            DateTime.UtcNow.AddMinutes(60), user.Role.ToString()));
    }
}

public record LogoutCommand(string RefreshToken) : IRequest<Result>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork uow)
    {
        _refreshTokens = refreshTokens;
        _uow = uow;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token is not null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            _refreshTokens.Update(token);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        return Result.Success();
    }
}
