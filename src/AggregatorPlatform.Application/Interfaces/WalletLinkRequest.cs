namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Demande de liaison d'un compte bancaire a un wallet.</summary>
/// <param name="PhoneNumber">Numero du wallet (obligatoire).</param>
/// <param name="PartnerRef">Reference d'idempotence cote partenaire (obligatoire, echo dans partnerTransactionRef).</param>
/// <param name="BankAccount">Numero de compte bancaire a lier au wallet (obligatoire).</param>
/// <param name="Extras">Donnees additionnelles libres.</param>
public record WalletLinkRequest(
    string PhoneNumber,
    string PartnerRef,
    string BankAccount,
    Dictionary<string, object?>? Extras = null);
