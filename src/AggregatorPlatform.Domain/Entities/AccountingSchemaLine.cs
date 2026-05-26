using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

/// <summary>
/// Template d'un mouvement comptable. A l'application du schema sur une transaction,
/// chaque ligne genere un Movement.
/// La formule du montant peut referencer les montants des lignes precedentes via L1, L2, ... LN
/// (N = LineOrder de la ligne deja calculee).
/// </summary>
public class AccountingSchemaLine : BaseEntity
{
    public Guid LineId { get; set; } = Guid.NewGuid();
    public Guid SchemaId { get; set; }

    /// <summary>Numero de ligne (sert d'index dans les formules : L1, L2, ...).</summary>
    public int LineOrder { get; set; }

    /// <summary>Code de compte statique (ignore si AccountType = Dynamic).</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Fixed : AccountCode tel quel ; Dynamic : evalue AccountExpression a l'execution.</summary>
    public AccountType AccountType { get; set; } = AccountType.Fixed;

    /// <summary>Expression resolvant le compte au runtime (ex: "CUSTOMER.PhoneNumber" ou "PARTNER.AccountCode").</summary>
    public string? AccountExpression { get; set; }

    /// <summary>Cote du mouvement (Debit / Credit).</summary>
    public LedgerSide Side { get; set; }

    /// <summary>Formule de calcul du montant (NCalc). Variables disponibles :
    /// AMOUNT, AMOUNT_NET, FEE, PARTNER.Balance, CUSTOMER.PhoneNumber, TX.Currency, TX.Type,
    /// L1, L2, ... LN (montants calcules des lignes precedentes).</summary>
    public string AmountFormula { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Code metier transmis au mouvement genere.</summary>
    public string? Code { get; set; }

    /// <summary>Exploitant transmis au mouvement genere.</summary>
    public string? Exploitant { get; set; }

    /// <summary>True : la ligne est ignoree si Condition est fausse.</summary>
    public bool IsConditional { get; set; }

    /// <summary>Condition NCalc (booleenne) evaluee dans le meme contexte que la formule du montant.</summary>
    public string? Condition { get; set; }

    /// <summary>True : ce mouvement contribue a Transaction.FeeAmount (sa valeur s'ajoute aux frais).</summary>
    public bool IsFee { get; set; }

    public AccountingSchema? Schema { get; set; }
}
