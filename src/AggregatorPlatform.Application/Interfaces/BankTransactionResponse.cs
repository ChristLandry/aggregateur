namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Reponse du connecteur bancaire pour POST /bank/transaction et POST /bank/insertmouvement
/// (meme shape). TransactionDate est en UTC (ISO-8601 cote wire). Aucun FailureReason
/// n'est fourni par le connecteur : quand <see cref="Success"/> vaut false, la raison
/// technique cote hub reste vide (l'appelant utilise le Status pour statuer).
/// Le champ <see cref="FailureReason"/> n'est peuple que par le fail-safe du BankApiClient
/// (exception HTTP, JSON invalide, circuit ouvert).
/// </summary>
public record BankTransactionResponse(
    bool Success,
    string TransactionBankIdentifier,
    DateTime TransactionDate,
    string? FailureReason = null);
