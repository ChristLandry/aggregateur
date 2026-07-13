using AggregatorPlatform.Domain.Common;

namespace AggregatorPlatform.Domain.Entities;

/// <summary>
/// Identite racine d'une personne physique dans la plateforme.
/// Un <see cref="Client"/> peut porter plusieurs <see cref="Customer"/> (un par
/// partenaire souscrit). L'unicite est portee par <see cref="BankAccountRoot"/>.
/// </summary>
public class Client : AuditableEntity
{
    public Guid ClientId { get; set; } = Guid.NewGuid();

    /// <summary>Racine du compte bancaire du client (identifiant metier stable).</summary>
    public string BankAccountRoot { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? NationalId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
