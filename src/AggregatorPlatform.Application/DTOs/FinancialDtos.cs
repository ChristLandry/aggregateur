using System.Text.Json;
using AggregatorPlatform.Domain.Enums;

// Direction (BTW/WTB) importee via using ci-dessus.

namespace AggregatorPlatform.Application.DTOs;

public record TransactionDto(
    Guid TransactionId,
    string PartnerTransactionRef,
    Guid PartnerId,
    Guid? SubscriptionId,
    Guid? CustomerId,
    TransactionType TransactionType,
    decimal Amount,
    decimal FeeAmount,
    decimal NetAmount,
    string Currency,
    TransactionStatus Status,
    string? FailureReason,
    AccountingStatus AccountingStatus,
    DateTime InitiatedAt,
    DateTime? CompletedAt,
    string? ExternalRef,
    string? BankAccount,
    string? PhoneNumber,
    string? ExtraData,
    string? OperationType);

/// <summary>
/// Payload generique d'initiation d'une transaction wallet (debit/credit/cancel).
/// La cible peut etre resolue soit via <see cref="SubscriptionId"/>, soit via
/// <see cref="BankAccount"/> / <see cref="PhoneNumber"/>. Pour les endpoints
/// /api/v1/bank/* utiliser <see cref="BankTransactionInitiateRequest"/>.
/// </summary>
public record TransactionRequest
{
    /// <summary>Reference d'idempotence cote partenaire (obligatoire).</summary>
    public string PartnerTransactionRef { get; init; } = string.Empty;

    /// <summary>Numero de compte bancaire cible.</summary>
    public string? BankAccount { get; init; }

    /// <summary>Numero de telephone wallet cible.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Abonnement client/partenaire (optionnel).</summary>
    public Guid? SubscriptionId { get; init; }

    /// <summary>Montant brut (obligatoire, > 0).</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Frais imposes par l'appelant (optionnel).
    /// Si null : calcule automatiquement via le schema comptable (lignes IsFee).
    /// Si renseigne : valeur respectee telle quelle (doit etre >= 0).
    /// </summary>
    public decimal? Fees { get; init; }

    /// <summary>Devise ISO-4217, 3 caracteres (obligatoire).</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Libelle libre.</summary>
    public string? Description { get; init; }

    /// <summary>Donnees additionnelles libres serialisees en JSON (optionnel).</summary>
    public JsonElement? ExtraData { get; init; }

    /// <summary>
    /// OperationType : type d'operation bancaire (BTW ou WTB). 
    /// Valeur optionnelle ; renseignee uniquement pour les transactions initiees via /api/v1/bank/debit.
    /// </summary>
    public string? OperationType { get; init; }
}

public record CancelTransactionRequest(string PartnerTransactionRef, string OriginalExternalRef);

/// <summary>
/// Payload d'initiation d'une transaction bancaire (/api/v1/bank/debit + /api/v1/bank/credit).
/// La souscription cible est resolue via le triplet (PartnerId, BankAccount, PhoneNumber) —
/// aucun SubscriptionId n'est accepte cote payload.
/// </summary>
public record BankTransactionInitiateRequest
{
    /// <summary>Reference d'idempotence cote partenaire (obligatoire).</summary>
    public string PartnerTransactionRef { get; init; } = string.Empty;

    /// <summary>Numero de compte bancaire cible (obligatoire).</summary>
    public string? BankAccount { get; init; }

    /// <summary>Numero de telephone associe a la souscription (obligatoire).</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Montant brut (obligatoire, > 0).</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Frais imposes par l'appelant (optionnel). Si null : calcule par le schema comptable.
    /// </summary>
    public decimal? Fees { get; init; }

    /// <summary>Devise ISO-4217, 3 caracteres (obligatoire).</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Libelle libre.</summary>
    public string? Description { get; init; }

    /// <summary>Donnees additionnelles libres serialisees en JSON (optionnel).</summary>
    public JsonElement? ExtraData { get; init; }

    /// <summary>
    /// OperationType : type d'operation bancaire (BTW ou WTB). Obligatoire pour /api/v1/bank/debit.
    /// </summary>
    public string? OperationType { get; init; }
}

public record BalanceDto(string Identifier, decimal Balance, string Currency, string Status);

/// <summary>Payload d'entree pour /api/v1/bank/balance.</summary>
/// <param name="BankAccount">Numero de compte bancaire (obligatoire).</param>
/// <param name="PhoneNumber">Numero de telephone associe a la souscription (obligatoire).</param>
public record BankBalanceRequest(string BankAccount, string PhoneNumber);

/// <summary>Requete de KYC wallet cote partenaire.</summary>
/// <param name="PhoneNumber">Numero du wallet (obligatoire).</param>
/// <param name="PartnerTemporalyCode">Code temporaire fourni par le partenaire (nonce, OTP, ...).</param>
/// <param name="Extras">Donnees additionnelles libres transmises par le partenaire.</param>
public record WalletKycRequest(
    string PhoneNumber,
    string? PartnerTemporalyCode,
    Dictionary<string, object?>? Extras);

/// <summary>Reponse KYC wallet : identite du client rattachee au wallet.</summary>
public record WalletKycDto(
    string PhoneNumber,
    string FullName,
    DateOnly DateOfBirth,
    string? NationalId);

/// <summary>
/// Requete de KYC bank (POST /api/v1/bank/kyc). Miroir du contrat connecteur
/// bank_connector : le seul champ requis est le compte bancaire. Les anciens
/// PartnerTemporalyCode / Extras ne sont plus consommes par le connecteur.
/// </summary>
public record BankKycRequest(string BankAccount);

/// <summary>
/// Reponse KYC bank : identite du client rattachee au compte bancaire.
/// Les champs proviennent tous du connecteur bank_connector — aucun n'est
/// fabrique cote hub. <see cref="DateOfBirth"/> est parse depuis la chaine
/// "yyyy-MM-dd" du connecteur ; si le parsing echoue, la valeur reste null.
/// </summary>
public record BankKycDto(
    string PhoneNumber,
    string FullName,
    DateOnly? DateOfBirth,
    string? IdType,
    string? NationalId);

/// <summary>
/// Reponse metier partenaire pour POST /api/v1/bank/balance.
/// Ne contient QUE ce que le connecteur bank_connector expose : l'identifiant
/// du compte interroge + le fond disponible. Pas de Currency, pas de Status.
/// </summary>
public record BankBalanceDto(string BankAccount, decimal FondDispo);

/// <summary>
/// Enveloppe de reponse specifique aux operations wallet transactionnelles
/// (link, unlink). Portee sur le fil, non enveloppee dans ApiResponse&lt;T&gt;.
/// </summary>
public record WalletOperationEnvelope(
    bool Success,
    Guid? TransactionId,
    string? PartnerTransactionRef,
    string? Status,
    object? Data,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime Timestamp);
