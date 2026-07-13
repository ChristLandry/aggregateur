using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Customer : AuditableEntity
{
    public Guid CustomerId { get; set; } = Guid.NewGuid();
    public string? ExternalCustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public KycStatus KycStatus { get; set; } = KycStatus.NotVerified;

    /// <summary>Client racine auquel appartient ce Customer (nullable pour retro-compat).</summary>
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
