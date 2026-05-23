using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class PartnerAccountMovement : BaseEntity
{
    public Guid MovementId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid? TransactionId { get; set; }
    public MovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public Partner? Partner { get; set; }
    public Transaction? Transaction { get; set; }
}
