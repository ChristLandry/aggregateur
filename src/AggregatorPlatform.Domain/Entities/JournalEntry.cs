using AggregatorPlatform.Domain.Common;

namespace AggregatorPlatform.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public Guid SchemaId { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }

    public Transaction? Transaction { get; set; }
    public AccountingSchema? Schema { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
