using System.Security.Cryptography;
using System.Text;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;

namespace AggregatorPlatform.API.Middleware;

public class PartnerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PartnerAuthMiddleware> _logger;

    private static readonly string[] BypassPaths =
    {
        "/api/v1/auth",
        "/health",
        "/metrics",
        "/swagger"
    };

    public PartnerAuthMiddleware(RequestDelegate next, ILogger<PartnerAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IPartnerRepository partners, ICacheService cache, IEncryptionService encryption)
    {
        var path = context.Request.Path.Value ?? "";
        if (BypassPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // RequirePartner attribute or specific controller paths trigger this check.
        // For simplicity here we enforce on /api/v1/customers, /subscriptions, /financial.
        var partnerScoped = path.StartsWith("/api/v1/customers", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/subscriptions", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/financial", StringComparison.OrdinalIgnoreCase);

        if (!partnerScoped)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Partner-Id", out var idValue) || !Guid.TryParse(idValue, out var partnerId))
        {
            await WriteError(context, 401, "MISSING_PARTNER_ID", "X-Partner-Id header is missing or invalid.");
            return;
        }

        var cacheKey = $"partner:{partnerId}";
        Partner? partner = await cache.GetAsync<Partner>(cacheKey, context.RequestAborted);
        if (partner is null)
        {
            partner = await partners.GetWithAccountAsync(partnerId, context.RequestAborted);
            if (partner is not null) await cache.SetAsync(cacheKey, partner, TimeSpan.FromSeconds(300), context.RequestAborted);
        }

        if (partner is null)
        {
            await WriteError(context, 401, "PARTNER_NOT_FOUND", "Partner not found.");
            return;
        }

        if (partner.Status != PartnerStatus.Active)
        {
            await WriteError(context, 401, "PARTNER_INACTIVE", "Partner is not active.");
            return;
        }

        var host = context.Request.Host.Host;
        if (!IsHostAllowed(host, partner.BaseUrl))
        {
            await WriteError(context, 401, "UNAUTHORIZED_URL", "Request host does not match partner base URL.");
            return;
        }

        if (partner.RequireHmac)
        {
            if (!await ValidateHmacAsync(context, partner, encryption))
            {
                await WriteError(context, 401, "INVALID_SIGNATURE", "HMAC signature is missing or invalid.");
                return;
            }
        }

        // Rate limiting (per minute)
        var minute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"ratelimit:{partner.PartnerId}:{minute}";
        var count = await cache.IncrementAsync(rateKey, TimeSpan.FromSeconds(70), context.RequestAborted);
        if (count > partner.RateLimitPerMin)
        {
            await WriteError(context, 429, "RATE_LIMIT_EXCEEDED", $"Rate limit of {partner.RateLimitPerMin}/min exceeded.");
            return;
        }

        context.Items["CurrentPartner"] = partner;
        using (_logger.BeginScope(new Dictionary<string, object> { ["PartnerId"] = partner.PartnerId }))
        {
            await _next(context);
        }
    }

    private static bool IsHostAllowed(string requestHost, string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) return true;
        return string.Equals(requestHost, uri.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> ValidateHmacAsync(HttpContext context, Partner partner, IEncryptionService encryption)
    {
        if (!context.Request.Headers.TryGetValue("X-Signature", out var sig)) return false;
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var expected = encryption.ComputeHmacSha256(body, partner.ApiKey);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig.ToString()));
    }

    private static Task WriteError(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(ApiResponse.Fail(code, message));
    }
}
