using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class Transaction : AuditableEntity
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public string PartnerTransactionRef { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }

    /// <summary>
    /// Abonnement source de la transaction. Nullable : une transaction peut etre initiee
    /// avec uniquement BankAccount / PhoneNumber sans abonnement enrole.
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Client porteur. Nullable : derive automatiquement de la subscription si renseignee,
    /// sinon laisse null pour les flux "anonymous-by-account".
    /// </summary>
    public Guid? CustomerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "XOF";
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; }
    public AccountingStatus AccountingStatus { get; set; } = AccountingStatus.Pending;
    public Guid? SchemaId { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ExternalRef { get; set; }

    /// <summary>Compte bancaire cible (chiffre AES-256 au repos).</summary>
    public string? BankAccount { get; set; }

    /// <summary>Numero de telephone wallet cible (chiffre AES-256 au repos).</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Donnees libres serialisees JSON (origine canal, metadata partenaire, etc.).</summary>
    public string? ExtraData { get; set; }

    /// <summary>
    /// OperationType de la transaction bancaire : BTW (debit vers wallet) ou WTB (credit depuis wallet).
    /// Nullable : renseigne uniquement pour les transactions initiees via /api/v1/bank/debit.
    /// </summary>
    public string? OperationType { get; set; }

    public Partner? Partner { get; set; }
    public Subscription? Subscription { get; set; }
    public Customer? Customer { get; set; }
    public AccountingSchema? Schema { get; set; }

    /// <summary>Mouvements comptables generes par l'application du schema.</summary>
    public ICollection<Movement> Movements { get; set; } = new List<Movement>();
}
