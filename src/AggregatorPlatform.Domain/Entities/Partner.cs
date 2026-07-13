using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Partner : AuditableEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string PartnerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public PartnerStatus Status { get; set; } = PartnerStatus.Inactive;
    public string Currency { get; set; } = "XOF";
    public string? WebhookUrl { get; set; }
    public string? IpWhitelist { get; set; }

    /// <summary>
    /// True : partenaire reserve a l'application web (frontoffice). Caracteristiques :
    /// - exclu de la liste publique GET /api/v1/partners ;
    /// - interdit d'appeler les routes financieres /api/v1/financial/{bank|wallet}/* ;
    /// - peut continuer a consommer les routes admin (transactions/movements) si role suffisant.
    /// </summary>
    public bool IsWebPartner { get; set; }

    /// <summary>Email de contact operationnel du partenaire (optionnel).</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Telephone de contact operationnel du partenaire (optionnel).</summary>
    public string? ContactPhone { get; set; }

    // ---- Alerte solde bas ----------------------------------------------
    /// <summary>Pourcentage (1-100) du <see cref="LowBalanceReferenceAmount"/> en dessous duquel une alerte doit etre envoyee.</summary>
    public int? LowBalanceThresholdPercent { get; set; }

    /// <summary>Montant de reference contre lequel le pourcentage est calcule.</summary>
    public decimal? LowBalanceReferenceAmount { get; set; }

    /// <summary>Canaux d'envoi (Email, Sms, ou combinaison). null / None = alertes desactivees.</summary>
    public AlertChannels? AlertChannels { get; set; }

    public PartnerAccount? PartnerAccount { get; set; }
    public ICollection<AccountingSchema> AccountingSchemas { get; set; } = new List<AccountingSchema>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
