namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Ligne de mouvement envoyee au connecteur bancaire (POST /bank/insertmouvement).
/// Miroir exact du DTO cote bank_connector (aucun champ ajoute ou renomme).
/// Le connecteur exige [MinLength(1)] sur le tableau : la caller garantit
/// qu'au moins une ligne est fournie.
/// </summary>
public record BankMouvementLine(
    string Account,
    string Label,
    string? Code,
    string? Exploitant,
    int LineOrder,
    string Reference,
    bool IsFee,
    DateTime TransactionDate,
    string TransactionId);
