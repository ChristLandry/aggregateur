using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Subscription : AuditableEntity
{
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid PartnerId { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneOperator { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public Customer? Customer { get; set; }
    public Partner? Partner { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
