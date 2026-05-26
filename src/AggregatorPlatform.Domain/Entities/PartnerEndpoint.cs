using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

/// <summary>
/// Lien de configuration partenaire <-> endpoint financier (+ schema comptable optionnel).
///
/// Deux niveaux de liaison materialises par une seule ligne :
///   1. Liaison Partner <-> EndpointKey : presence de la ligne = partenaire eligible.
///   2. Liaison (Partner+EndpointKey) <-> AccountingSchema : valeur de SchemaId.
///      SchemaId est nullable et peut etre detache (mise a null) sans supprimer
///      la liaison de premier niveau.
///
/// Unicite : (PartnerId, EndpointKey) — un seul lien actif par couple.
/// </summary>
public class PartnerEndpoint : AuditableEntity
{
    public Guid PartnerEndpointId { get; set; } = Guid.NewGuid();

    public Guid PartnerId { get; set; }
    public FinancialEndpointKey EndpointKey { get; set; }

    /// <summary>Schema comptable attache a ce lien (nullable, detachable a volonte).</summary>
    public Guid? SchemaId { get; set; }

    public Partner? Partner { get; set; }
    public AccountingSchema? Schema { get; set; }
}
