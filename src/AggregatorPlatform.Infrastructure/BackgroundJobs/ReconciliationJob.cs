using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.BackgroundJobs;

public class ReconciliationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReconciliationJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public ReconciliationJob(IServiceProvider services, ILogger<ReconciliationJob> logger)
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
                await ReconcileOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReconciliationJob iteration failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); } catch (TaskCanceledException) { return; }
        }
    }

    private async Task ReconcileOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var txs = sp.GetRequiredService<ITransactionRepository>();
        var partners = sp.GetRequiredService<IPartnerRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var accounting = sp.GetRequiredService<IAccountingEngine>();
        var bank = sp.GetRequiredService<IBankApiClient>();
        var wallet = sp.GetRequiredService<IWalletApiClient>();

        var threshold = DateTime.UtcNow.AddMinutes(-30);
        var pending = await txs.GetPendingOlderThanAsync(threshold, ct);
        _logger.LogInformation("Reconciliation: {Count} pending transactions to check.", pending.Count);

        var processed = 0;
        var ok = 0;
        var failed = 0;

        foreach (var tx in pending)
        {
            if (ct.IsCancellationRequested) break;
            var partner = await partners.GetByIdAsync(tx.PartnerId, ct);
            if (partner is null || string.IsNullOrEmpty(tx.ExternalRef)) continue;

            try
            {
                string statusStr;
                string? failureReason;
                if (tx.TransactionType is TransactionType.BankDebit or TransactionType.BankCredit)
                {
                    var resp = await bank.GetStatusAsync(partner, tx.ExternalRef, ct);
                    statusStr = resp.Status;
                    failureReason = resp.FailureReason;
                }
                else
                {
                    var resp = await wallet.GetStatusAsync(partner, tx.ExternalRef, ct);
                    statusStr = resp.Status;
                    failureReason = resp.FailureReason;
                }

                if (statusStr.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    tx.Status = TransactionStatus.Success;
                    tx.CompletedAt = DateTime.UtcNow;
                    txs.Update(tx);
                    await accounting.ApplyAsync(tx, ct);
                    await uow.SaveChangesAsync(ct);
                    ok++;
                }
                else if (statusStr.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                {
                    tx.Status = TransactionStatus.Failed;
                    tx.FailureReason = failureReason;
                    tx.CompletedAt = DateTime.UtcNow;
                    txs.Update(tx);
                    await uow.SaveChangesAsync(ct);
                    failed++;
                }
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconciliation: timeout/error on tx {TxId}", tx.TransactionId);
            }
        }

        _logger.LogInformation("Reconciliation report: processed={Processed} success={Ok} failed={Failed}", processed, ok, failed);
    }
}
