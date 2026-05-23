using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class FeeConfiguration : BaseEntity
{
    public Guid FeeId { get; set; } = Guid.NewGuid();
    public Guid? PartnerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public FeeType FeeType { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal Percentage { get; set; }
    public decimal? MaxFeeAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public Partner? Partner { get; set; }
}
