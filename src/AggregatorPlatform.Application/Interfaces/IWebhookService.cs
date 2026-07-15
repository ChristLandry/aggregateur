using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IWebhookService
{
    Task EnqueueAsync(Guid partnerId, Guid? transactionId, string eventType, object payload, CancellationToken cancellationToken = default);
    Task<bool> DispatchAsync(WebhookLog webhook, CancellationToken cancellationToken = default);
}
