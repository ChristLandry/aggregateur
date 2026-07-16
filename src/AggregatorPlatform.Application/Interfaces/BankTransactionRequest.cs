namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Payload envoye au connecteur bancaire pour un debit/credit.
/// - BankAccount : compte cible cote banque (identifiant du client).
/// - Codopsc : code operation (ex. BTW/WTB pour le sens transfert-banque/wallet)
///   ou code metier issu du schema comptable ; consomme par la banque pour router.
/// - Fees : frais applicables, calcules par le hub ou surchargeables par le payload.
/// </summary>
public record BankTransactionRequest(
    string PartnerRef,
    string BankAccount,
    string? Codopsc,
    decimal Amount,
    decimal Fees,
    string Currency,
    string? Description);
