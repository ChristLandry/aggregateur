using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

public record CustomerDto(
    Guid CustomerId,
    string? ExternalCustomerId,
    string FullName,
    DateOnly DateOfBirth,
    string? Email,
    CustomerStatus Status,
    KycStatus KycStatus,
    DateTime CreatedAt);

public record CreateCustomerRequest(
    string? ExternalCustomerId,
    string FullName,
    DateOnly DateOfBirth,
    string? NationalId,
    string? Email);

/// <summary>
/// Payload PATCH partiel : seules les proprietes renseignees (non-null) sont
/// appliquees a l'entite. Une valeur omise reste a null et la valeur existante
/// en BD est preservee.
/// </summary>
public record UpdateCustomerRequest(
    string? FullName,
    DateOnly? DateOfBirth,
    string? Email,
    CustomerStatus? Status,
    KycStatus? KycStatus);

public record SubscriptionDto(
    Guid SubscriptionId,
    Guid CustomerId,
    Guid PartnerId,
    string PhoneNumber,
    string PhoneOperator,
    SubscriptionStatus Status,
    DateTime SubscribedAt,
    DateTime? ExpiresAt);

public record CreateSubscriptionRequest(
    string BankAccountNumber,
    string PhoneNumber,
    string PhoneOperator,
    DateTime? ExpiresAt);

/// <summary>
/// Payload pour la creation directe d'une souscription via POST /api/v1/subscriptions.
/// Le PartnerId N'EST PAS dans le payload : il est resolu exclusivement depuis le header
/// X-Partner-Id (middleware PartnerAuth). Toute tentative de surcharge cote client est ignoree.
/// </summary>
public record CreateSubscriptionDirectRequest(
    Guid CustomerId,
    string BankAccountNumber,
    string PhoneNumber,
    string PhoneOperator,
    DateTime? ExpiresAt);

public record ChangeSubscriptionStatusRequest(SubscriptionStatus Status);
