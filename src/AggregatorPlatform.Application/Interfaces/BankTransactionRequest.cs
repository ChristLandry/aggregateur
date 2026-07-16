namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Payload envoye au connecteur bancaire pour POST /bank/transaction.
/// Le sens (BTW = debit banque, WTB = credit banque) est porte par
/// <see cref="CodOpsc"/> ; il n'y a qu'un seul endpoint cote connecteur.
/// TransactionId = reference partenaire cote hub (idempotence).
/// </summary>
public record BankTransactionRequest(
    string BankAccount,
    string CodOpsc,
    decimal Amount,
    decimal Fees,
    string TransactionId);
