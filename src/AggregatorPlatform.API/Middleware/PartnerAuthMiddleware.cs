using System.Security.Cryptography;
using System.Text;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;

namespace AggregatorPlatform.API.Middleware;

/// <summary>
/// Authentification partenaire via le header <b>X-Partner-ApiKey</b> :
/// la cle API en clair est hashee en SHA-256 puis comparee au champ Partner.ApiKey
/// (stocke deja hashe en BD). En cas de succes, le Partner est mis en cache et
/// expose via <see cref="ICurrentPartnerService"/>.
///
/// Routes partner-scoped : /api/v1/subscriptions, /api/v1/financial
/// (sauf certaines routes admin sous /financial/transactions* qui passent
/// par le JWT classique).
/// </summary>
public class PartnerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PartnerAuthMiddleware> _logger;

    private static readonly string[] BypassPaths =
    {
        "/api/v1/auth",
        "/health",
        "/metrics",
        "/swagger",
    };

    /// <summary>Sous-routes de /financial qui restent administrables au JWT, sans X-Partner-ApiKey.</summary>
    private static readonly string[] FinancialAdminPaths =
    {
        "/api/v1/financial/transactions",
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

        var partnerScoped =
            path.StartsWith("/api/v1/subscriptions", StringComparison.OrdinalIgnoreCase)
            || (path.StartsWith("/api/v1/financial", StringComparison.OrdinalIgnoreCase)
                && !FinancialAdminPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        if (!partnerScoped)
        {
            await _next(context);
            return;
        }

        // 1) Lecture du header X-Partner-ApiKey (cle en clair).
        if (!context.Request.Headers.TryGetValue("X-Partner-ApiKey", out var apiKeyValues))
        {
            await WriteError(context, 401, "MISSING_PARTNER_APIKEY",
                "X-Partner-ApiKey header is missing.");
            return;
        }
        var apiKey = apiKeyValues.ToString().Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteError(context, 401, "MISSING_PARTNER_APIKEY",
                "X-Partner-ApiKey header is empty.");
            return;
        }

        // 2) Hash SHA-256 et lookup en cache puis en BD.
        var apiKeyHash = encryption.ComputeSha256(apiKey);
        var cacheKey = $"partner:apikey:{apiKeyHash}";

        Partner? partner = await cache.GetAsync<Partner>(cacheKey, context.RequestAborted);
        if (partner is null)
        {
            partner = await partners.GetByApiKeyHashAsync(apiKeyHash, context.RequestAborted);
            if (partner is not null)
                await cache.SetAsync(cacheKey, partner, TimeSpan.FromSeconds(300), context.RequestAborted);
        }

        if (partner is null)
        {
            await WriteError(context, 401, "INVALID_PARTNER_APIKEY",
                "Unknown or revoked partner API key.");
            return;
        }

        if (partner.Status != PartnerStatus.Active)
        {
            await WriteError(context, 401, "PARTNER_INACTIVE",
                "Partner is not active.");
            return;
        }

        if (partner.RequireHmac)
        {
            if (!await ValidateHmacAsync(context, partner, encryption, apiKey))
            {
                await WriteError(context, 401, "INVALID_SIGNATURE",
                    "HMAC signature is missing or invalid.");
                return;
            }
        }

        // 3) Rate limiting par minute (clef Partner+minute).
        var minute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"ratelimit:{partner.PartnerId}:{minute}";
        var count = await cache.IncrementAsync(rateKey, TimeSpan.FromSeconds(70), context.RequestAborted);
        if (count > partner.RateLimitPerMin)
        {
            await WriteError(context, 429, "RATE_LIMIT_EXCEEDED",
                $"Rate limit of {partner.RateLimitPerMin}/min exceeded.");
            return;
        }

        context.Items["CurrentPartner"] = partner;
        using (_logger.BeginScope(new Dictionary<string, object> { ["PartnerId"] = partner.PartnerId }))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Valide la signature HMAC-SHA256 du body avec la cle API EN CLAIR
    /// (transmise par le partenaire dans X-Partner-ApiKey).
    /// </summary>
    private static async Task<bool> ValidateHmacAsync(HttpContext context, Partner partner, IEncryptionService encryption, string apiKeyClear)
    {
        if (!context.Request.Headers.TryGetValue("X-Signature", out var sig)) return false;
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var expected = encryption.ComputeHmacSha256(body, apiKeyClear);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig.ToString()));
    }

    private static Task WriteError(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(ApiResponse.Fail(code, message));
    }
}
