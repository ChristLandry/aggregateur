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
            // WEB : toujours realigner le hash sur Web:PartnerApiKey (source de verite
            // partagee avec NEXT_PUBLIC_WEB_PARTNER_APIKEY cote front).
            existing.ApiKey = apiKeyHash;
            existing.ApiKeyPlaintext = clearApiKey;
            if (!existing.IsWebPartner) { existing.IsWebPartner = true; }
            if (existing.Status != PartnerStatus.Active) { existing.Status = PartnerStatus.Active; }

            // Compte miroir obligatoire : sans PartnerAccount, GetByApiKeyHashAsync
            // (Include required 1-1) peut exclure le partenaire WEB du resultat.
            var hasAccount = await db.PartnerAccounts.IgnoreQueryFilters()
                .AnyAsync(a => a.PartnerId == existing.PartnerId, ct);
            if (!hasAccount)
            {
                db.PartnerAccounts.Add(new PartnerAccount
                {
                    PartnerId = existing.PartnerId,
                    PartnerBankAccount = string.Empty,
                    Balance = 0,
                    Currency = existing.Currency ?? "XOF",
                });
                logger.LogInformation("Compte miroir WEB cree (PartnerId={Id}).", existing.PartnerId);
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Partenaire WEB resynchronise (PartnerId={Id}, ApiKey aligne sur Web:PartnerApiKey).",
                existing.PartnerId);
        }
    }
}
