using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

/// <summary>
/// Mouvement comptable genere a partir d'une ligne d'un schema applique a une transaction.
/// "ligne du schema" = "mouvement" dans le vocabulaire metier.
/// Une transaction produit N mouvements selon le schema applique.
/// </summary>
public class Movement : BaseEntity
{
    public Guid MovementId { get; set; } = Guid.NewGuid();

    /// <summary>Transaction qui a genere ce mouvement.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Schema applique.</summary>
    public Guid SchemaId { get; set; }

    /// <summary>Numero d'ordre dans le schema (correspond a AccountingSchemaLine.LineOrder).
    /// Utilise pour referencer ce mouvement dans les formules des lignes suivantes via L1, L2, ...</summary>
    public int LineOrder { get; set; }

    /// <summary>Compte impacte (peut etre resolu dynamiquement = compte client / commission / partenaire).</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>Montant signe : convention amount &lt; 0 = debit (sortant), amount &gt; 0 = credit (entrant).</summary>
    public decimal Amount { get; set; }

    /// <summary>Cote comptable explicite (Debit/Credit) — duplique l'information du signe.</summary>
    public LedgerSide Side { get; set; }

    /// <summary>Libelle du mouvement (recopie depuis AccountingSchemaLine.Label).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Code metier (recopie depuis AccountingSchemaLine.Code).</summary>
    public string? Code { get; set; }

    /// <summary>Exploitant (recopie depuis AccountingSchemaLine.Exploitant).</summary>
    public string? Exploitant { get; set; }

    /// <summary>Reference libre (souvent = PartnerTransactionRef ou ExternalRef de la transaction).</summary>
    public string? Reference { get; set; }

    /// <summary>Date comptable du mouvement (par defaut = date d'enregistrement).</summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    /// <summary>Marqueur : ce mouvement represente des frais (alimente Transaction.FeeAmount).</summary>
    public bool IsFee { get; set; }

    public Transaction? Transaction { get; set; }
    public AccountingSchema? Schema { get; set; }
}
