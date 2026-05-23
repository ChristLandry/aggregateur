using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class WebhookLog : BaseEntity
{
    public Guid LogId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid? TransactionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public WebhookStatus Status { get; set; } = WebhookStatus.Pending;
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }

    public Partner? Partner { get; set; }
    public Transaction? Transaction { get; set; }
}
