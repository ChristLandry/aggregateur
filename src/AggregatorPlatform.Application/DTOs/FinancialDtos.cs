using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

public record TransactionDto(
    Guid TransactionId,
    string PartnerTransactionRef,
    Guid PartnerId,
    Guid SubscriptionId,
    Guid CustomerId,
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
    string? ExternalRef);

public record TransactionRequest(
    string PartnerTransactionRef,
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    string? Description);

public record CancelTransactionRequest(string PartnerTransactionRef, string OriginalExternalRef);

public record BalanceQueryRequest(Guid SubscriptionId);

public record BalanceDto(string Identifier, decimal Balance, string Currency, string Status);

public record KycDto(string Identifier, string FullName, string Status, string KycLevel);
