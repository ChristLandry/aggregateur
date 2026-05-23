using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.BackgroundJobs;

public class WebhookDispatchJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WebhookDispatchJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    public WebhookDispatchJob(IServiceProvider services, ILogger<WebhookDispatchJob> logger)
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
                using var scope = _services.CreateScope();
                var logs = scope.ServiceProvider.GetRequiredService<IWebhookLogRepository>();
                var sender = scope.ServiceProvider.GetRequiredService<IWebhookService>();

                var pending = await logs.GetPendingAsync(3, stoppingToken);
                foreach (var w in pending)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    await sender.DispatchAsync(w, stoppingToken);
                }

                if (pending.Count > 0)
                    _logger.LogInformation("Webhook dispatch: {Count} attempts.", pending.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebhookDispatchJob iteration failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); } catch (TaskCanceledException) { return; }
        }
    }
}
