using AggregatorPlatform.Domain.Common;

namespace AggregatorPlatform.Domain.Entities;

public class PartnerAccount : BaseEntity
{
    public Guid AccountId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }

    /// <summary>
    /// Numero de compte bancaire du partenaire utilise pour le reglement.
    /// Chiffre AES-256 au repos.
    /// </summary>
    public string PartnerBankAccount { get; set; } = string.Empty;

    public decimal Balance { get; set; }
    public string Currency { get; set; } = "XOF";
    public DateTime? LastMovementAt { get; set; }

    public Partner? Partner { get; set; }
    public ICollection<PartnerAccountMovement> Movements { get; set; } = new List<PartnerAccountMovement>();
}
