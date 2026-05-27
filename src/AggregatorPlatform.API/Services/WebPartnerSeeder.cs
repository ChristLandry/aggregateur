using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using AggregatorPlatform.Infrastructure.Persistence;

namespace AggregatorPlatform.API.Services;

/// <summary>
/// Cree/met a jour le partenaire technique <b>WEB</b> au demarrage.
/// La cle API en clair est lue depuis la config (Web:PartnerApiKey) ;
/// son SHA-256 est stocke dans Partner.ApiKey. Le partenaire est marque
/// IsWebPartner=true (filtre du listing + interdit sur /financial/{bank|wallet}/*).
/// </summary>
public static class WebPartnerSeeder
{
    private const string WebPartnerCode = "WEB";

    public static async Task EnsureWebPartnerAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var clearApiKey = configuration["Web:PartnerApiKey"];
        if (string.IsNullOrWhiteSpace(clearApiKey))
        {
            logger.LogWarning("Web:PartnerApiKey absent de la configuration. Auto-seed du partenaire WEB ignore.");
            return;
        }

        var db = services.GetRequiredService<AggregatorDbContext>();
        var encryption = services.GetRequiredService<IEncryptionService>();
        var apiKeyHash = encryption.ComputeSha256(clearApiKey);

        var existing = await db.Partners.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PartnerCode == WebPartnerCode, ct);

        if (existing is null)
        {
            var partner = new Partner
            {
                PartnerCode = WebPartnerCode,
                Name = "Application Web (interne)",
                BaseUrl = configuration["Web:BaseUrl"] ?? "http://localhost:3000",
                ApiKey = apiKeyHash,
                AccountCode = "P-WEB",
                Status = PartnerStatus.Active,
                Currency = "XOF",
                RateLimitPerMin = 6000, // largesse pour un client interne
                RequireHmac = false,
                IsWebPartner = true,
            };
            db.Partners.Add(partner);

            // Compte miroir minimal (necessaire pour l'unicite 1-1 Partner-PartnerAccount).
            db.PartnerAccounts.Add(new PartnerAccount
            {
                PartnerId = partner.PartnerId,
                PartnerBankAccount = string.Empty,
                Balance = 0,
                Currency = "XOF",
            });

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Partenaire WEB cree (PartnerId={Id}).", partner.PartnerId);
        }
        else
        {
            // Re-synchronise les invariants (flag + hash + statut).
            var changed = false;
            if (!existing.IsWebPartner) { existing.IsWebPartner = true; changed = true; }
            if (existing.Status != PartnerStatus.Active) { existing.Status = PartnerStatus.Active; changed = true; }
            if (existing.ApiKey != apiKeyHash) { existing.ApiKey = apiKeyHash; changed = true; }
            if (changed)
            {
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Partenaire WEB resynchronise (PartnerId={Id}).", existing.PartnerId);
            }
        }
    }
}
