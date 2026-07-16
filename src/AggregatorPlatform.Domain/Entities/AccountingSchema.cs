using AggregatorPlatform.Domain.Common;
using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Domain.Entities;

public class AccountingSchema : AuditableEntity
{
    public Guid SchemaId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransactionSide TransactionSide { get; set; }
    public Channel Channel { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Description { get; set; }

    /// <summary>
    /// Vrai : le schema est gere directement par la banque (bank-managed) ;
    /// le hub delegue l'ecriture comptable au connecteur bancaire, sans generer
    /// de Movements localement.
    /// Faux (defaut) : le schema est gere par le hub. Les Movements sont produits
    /// par l'AccountingEngine AVANT l'appel au connecteur bancaire, et le solde
    /// miroir du partenaire est ajuste localement.
    /// </summary>
    public bool IsBankManaged { get; set; } = false;

    public Partner? Partner { get; set; }
    public ICollection<AccountingSchemaLine> Lines { get; set; } = new List<AccountingSchemaLine>();
}
