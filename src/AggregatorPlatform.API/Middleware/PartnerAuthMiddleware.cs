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
/// Routes partner-scoped : /api/v1/subscriptions, /api/v1/bank, /api/v1/wallet
/// (et /api/v1/financial sauf /financial/transactions* admin JWT).
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
            || path.StartsWith("/api/v1/bank", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/wallet", StringComparison.OrdinalIgnoreCase)
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

        // Un partenaire WEB ne peut pas appeler les routes operationnelles bank/wallet
        // (ni l'ancien prefixe /api/v1/financial/{bank|wallet}/* s'il restait expose).
        // Les routes admin /api/v1/financial/transactions/* sont deja exemptees plus haut.
        if (partner.IsWebPartner &&
            (path.StartsWith("/api/v1/bank", StringComparison.OrdinalIgnoreCase)
             || path.StartsWith("/api/v1/wallet", StringComparison.OrdinalIgnoreCase)
             || path.StartsWith("/api/v1/financial/", StringComparison.OrdinalIgnoreCase)))
        {
            await WriteError(context, 403, "WEB_PARTNER_FORBIDDEN",
                "The WEB partner cannot access financial endpoints.");
            return;
        }

        context.Items["CurrentPartner"] = partner;
        using (_logger.BeginScope(new Dictionary<string, object> { ["PartnerId"] = partner.PartnerId }))
        {
            await _next(context);
        }
    }

    private static Task WriteError(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(ApiResponse.Fail(code, message));
    }
}
