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
/// Payload generique d'initiation d'une transaction (debit/credit, bank/wallet).
/// </summary>
/// <remarks>
/// Au moins un identifiant de cible est requis :
///   - <see cref="SubscriptionId"/> (cas standard : l'abonnement portait deja le compte et/ou le numero),
///   - ou <see cref="BankAccount"/> / <see cref="PhoneNumber"/> (cible explicite hors abonnement).
/// </remarks>
public record TransactionRequest
{
    /// <summary>Reference d'idempotence cote partenaire (obligatoire).</summary>
    public string PartnerTransactionRef { get; init; } = string.Empty;

    /// <summary>Numero de compte bancaire cible (alimente la transaction si pas de subscription).</summary>
    public string? BankAccount { get; init; }

    /// <summary>Numero de telephone wallet cible (alimente la transaction si pas de subscription).</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Abonnement client/partenaire — optionnel : si fourni, BankAccount/PhoneNumber sont resolus depuis lui.</summary>
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

public record BalanceDto(string Identifier, decimal Balance, string Currency, string Status);

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

/// <summary>Requete de KYC bank cote partenaire (POST /api/v1/bank/kyc).</summary>
/// <param name="AccountNumber">Numero de compte bancaire (obligatoire).</param>
/// <param name="PartnerTemporalyCode">Code temporaire fourni par le partenaire (nonce/OTP).</param>
/// <param name="Extras">Donnees additionnelles libres transmises par le partenaire.</param>
public record BankKycRequest(
    string AccountNumber,
    string? PartnerTemporalyCode,
    Dictionary<string, object?>? Extras);

/// <summary>Reponse KYC bank : identite du client rattachee au compte bancaire.</summary>
public record BankKycDto(
    string AccountNumber,
    string FullName,
    DateOnly? DateOfBirth,
    string? NationalId,
    string? PhoneNumber);

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
