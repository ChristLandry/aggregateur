using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Partner : AuditableEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string PartnerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public PartnerStatus Status { get; set; } = PartnerStatus.Inactive;
    public string Currency { get; set; } = "XOF";
    public string? WebhookUrl { get; set; }
    public int RateLimitPerMin { get; set; } = 100;
    public string? IpWhitelist { get; set; }
    public bool RequireHmac { get; set; }

    public PartnerAccount? PartnerAccount { get; set; }
    public ICollection<AccountingSchema> AccountingSchemas { get; set; } = new List<AccountingSchema>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
