using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IAccountingEngine
{
    /// <summary>
    /// Applique le schema comptable a la transaction : genere les Movements localement
    /// puis met a jour le compte miroir du partenaire. Utilise en flow hub-managed.
    /// </summary>
    Task ApplyAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met a jour UNIQUEMENT le compte miroir du partenaire (pas de Movements locaux).
    /// Utilise en flow bank-managed apres succes du connecteur bancaire : l'ecriture
    /// comptable est deleguee a la banque mais le hub doit tout de meme reflechir le
    /// solde du miroir (BTW -> debit, WTB -> credit d'apres tx.OperationType).
    /// </summary>
    Task ApplyMirrorAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
