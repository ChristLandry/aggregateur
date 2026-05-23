using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly IWebhookLogRepository _logs;
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _uow;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(IWebhookLogRepository logs, IPartnerRepository partners, IUnitOfWork uow,
        IHttpClientFactory httpFactory, IEncryptionService encryption, ILogger<WebhookService> logger)
    {
        _logs = logs;
        _partners = partners;
        _uow = uow;
        _httpFactory = httpFactory;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task EnqueueAsync(Guid partnerId, Guid? transactionId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var partner = await _partners.GetByIdAsync(partnerId, cancellationToken);
        if (partner is null || string.IsNullOrEmpty(partner.WebhookUrl))
        {
            _logger.LogDebug("Webhook skipped: partner {PartnerId} has no webhook URL.", partnerId);
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        await _logs.AddAsync(new WebhookLog
        {
            PartnerId = partnerId,
            TransactionId = transactionId,
            EventType = eventType,
            Payload = json,
            TargetUrl = partner.WebhookUrl,
            Status = WebhookStatus.Pending,
            NextAttemptAt = DateTime.UtcNow
        }, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DispatchAsync(WebhookLog webhook, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("Webhook");
        var partner = await _partners.GetByIdAsync(webhook.PartnerId, cancellationToken);
        var signature = partner is not null ? _encryption.ComputeHmacSha256(webhook.Payload, partner.ApiKey) : "";
        var content = new StringContent(webhook.Payload, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature", signature);
        content.Headers.Add("X-Event-Type", webhook.EventType);

        webhook.AttemptCount++;
        webhook.LastAttemptAt = DateTime.UtcNow;

        try
        {
            var response = await client.PostAsync(webhook.TargetUrl, content, cancellationToken);
            webhook.ResponseCode = (int)response.StatusCode;
            webhook.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                webhook.Status = WebhookStatus.Delivered;
                _logs.Update(webhook);
                await _uow.SaveChangesAsync(cancellationToken);
                return true;
            }

            if (webhook.AttemptCount >= 3)
            {
                webhook.Status = WebhookStatus.Failed;
            }
            else
            {
                var backoff = TimeSpan.FromMinutes(webhook.AttemptCount == 1 ? 1 : webhook.AttemptCount == 2 ? 5 : 15);
                webhook.NextAttemptAt = DateTime.UtcNow.Add(backoff);
            }
            _logs.Update(webhook);
            await _uow.SaveChangesAsync(cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch failed for {LogId}", webhook.LogId);
            webhook.ResponseBody = ex.Message;
            if (webhook.AttemptCount >= 3) webhook.Status = WebhookStatus.Failed;
            else
            {
                var backoff = TimeSpan.FromMinutes(webhook.AttemptCount == 1 ? 1 : webhook.AttemptCount == 2 ? 5 : 15);
                webhook.NextAttemptAt = DateTime.UtcNow.Add(backoff);
            }
            _logs.Update(webhook);
            await _uow.SaveChangesAsync(cancellationToken);
            return false;
        }
    }
}
