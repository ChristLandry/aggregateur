using AggregatorPlatform.Domain.Common;

namespace AggregatorPlatform.Domain.Entities;

public class PartnerAccount : BaseEntity
{
    public Guid AccountId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "XOF";
    public DateTime? LastMovementAt { get; set; }

    public Partner? Partner { get; set; }
    public ICollection<PartnerAccountMovement> Movements { get; set; } = new List<PartnerAccountMovement>();
}
