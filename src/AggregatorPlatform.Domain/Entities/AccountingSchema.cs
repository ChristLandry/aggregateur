using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class AccountingSchema : AuditableEntity
{
    public Guid SchemaId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransactionSide TransactionSide { get; set; }
    public Channel Channel { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Description { get; set; }

    public Partner? Partner { get; set; }
    public ICollection<AccountingSchemaLine> Lines { get; set; } = new List<AccountingSchemaLine>();
}
