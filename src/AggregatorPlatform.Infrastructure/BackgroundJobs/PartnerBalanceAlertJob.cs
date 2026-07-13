using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AggregatorPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Job hourly qui parcourt les partenaires ayant configure une alerte sur leur solde bas
/// (<see cref="Partner.LowBalanceThresholdPercent"/> + <see cref="Partner.LowBalanceReferenceAmount"/> +
/// <see cref="Partner.AlertChannels"/>) et envoie une notification via Email/SMS quand
/// <c>Balance &lt;= Reference * Percent / 100</c>.
/// </summary>
public class PartnerBalanceAlertJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<PartnerBalanceAlertJob> _logger;

    public PartnerBalanceAlertJob(IServiceProvider services, ILogger<PartnerBalanceAlertJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PartnerBalanceAlertJob iteration failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); } catch (TaskCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AggregatorDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var sms = scope.ServiceProvider.GetRequiredService<ISmsSender>();

        // Uniquement les partenaires avec alertes configurees et un canal actif.
        var eligibles = await db.Partners
            .Where(p => p.Status == PartnerStatus.Active
                        && p.LowBalanceThresholdPercent.HasValue
                        && p.LowBalanceReferenceAmount.HasValue
                        && p.AlertChannels.HasValue
                        && p.AlertChannels != AlertChannels.None)
            .Select(p => new
            {
                p.PartnerId,
                p.PartnerCode,
                p.Name,
                p.ContactEmail,
                p.ContactPhone,
                p.LowBalanceThresholdPercent,
                p.LowBalanceReferenceAmount,
                p.AlertChannels,
                Balance = p.PartnerAccount != null ? p.PartnerAccount.Balance : 0m,
                Currency = p.PartnerAccount != null ? p.PartnerAccount.Currency : p.Currency,
            })
            .ToListAsync(ct);

        var triggered = 0;
        foreach (var p in eligibles)
        {
            var threshold = p.LowBalanceReferenceAmount!.Value * p.LowBalanceThresholdPercent!.Value / 100m;
            if (p.Balance > threshold) continue;

            var subject = $"[Aggregator] Solde bas – {p.PartnerCode}";
            var body = $"Le solde du partenaire {p.Name} ({p.PartnerCode}) est de {p.Balance:F2} {p.Currency}, " +
                       $"seuil configure = {p.LowBalanceThresholdPercent}% de {p.LowBalanceReferenceAmount:F2} {p.Currency} " +
                       $"= {threshold:F2} {p.Currency}.";

            var channels = p.AlertChannels!.Value;
            if (channels.HasFlag(AlertChannels.Email) && !string.IsNullOrWhiteSpace(p.ContactEmail))
                await email.SendAsync(p.ContactEmail, subject, body, ct);
            if (channels.HasFlag(AlertChannels.Sms) && !string.IsNullOrWhiteSpace(p.ContactPhone))
                await sms.SendAsync(p.ContactPhone, body, ct);

            triggered++;
        }

        if (eligibles.Count > 0)
        {
            _logger.LogInformation("PartnerBalanceAlertJob: {Triggered}/{Eligibles} alertes envoyees.",
                triggered, eligibles.Count);
        }
    }
}
