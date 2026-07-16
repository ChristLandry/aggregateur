namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Reponse du connecteur bancaire pour POST /bank/balance.
/// Le connecteur ne renvoie QUE le fond disponible ; aucun champ Currency
/// ou Status n'est produit (cf. contrat bank_connector). Toute valeur
/// supplementaire cote hub serait mockee et est interdite.
/// </summary>
public record BankBalanceResponse(decimal FondDispo);
