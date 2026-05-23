using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Transaction : AuditableEntity
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public string PartnerTransactionRef { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid CustomerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "XOF";
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; }
    public AccountingStatus AccountingStatus { get; set; } = AccountingStatus.Pending;
    public Guid? SchemaId { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ExternalRef { get; set; }

    public Partner? Partner { get; set; }
    public Subscription? Subscription { get; set; }
    public Customer? Customer { get; set; }
    public AccountingSchema? Schema { get; set; }
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
