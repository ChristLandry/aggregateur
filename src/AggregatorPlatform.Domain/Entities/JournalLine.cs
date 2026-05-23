using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class JournalLine : BaseEntity
{
    public Guid LineId { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public LedgerSide Side { get; set; }
    public decimal Amount { get; set; }
    public string Label { get; set; } = string.Empty;

    public JournalEntry? Entry { get; set; }
}
