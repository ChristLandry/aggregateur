using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class AccountingSchemaLine : BaseEntity
{
    public Guid LineId { get; set; } = Guid.NewGuid();
    public Guid SchemaId { get; set; }
    public int LineOrder { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Fixed;
    public string? AccountExpression { get; set; }
    public LedgerSide Side { get; set; }
    public string AmountFormula { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsConditional { get; set; }
    public string? Condition { get; set; }

    public AccountingSchema? Schema { get; set; }
}
