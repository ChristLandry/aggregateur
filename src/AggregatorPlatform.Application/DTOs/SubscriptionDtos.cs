using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

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
    string BankAccount,
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
    string BankAccount,
    string PhoneNumber,
    string PhoneOperator,
    DateTime? ExpiresAt);

public record ChangeSubscriptionStatusRequest(SubscriptionStatus Status);
