using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

public record TransactionReportItemDto(
    Guid TransactionId,
    string PartnerTransactionRef,
    string PartnerCode,
    TransactionType Type,
    decimal Amount,
    decimal FeeAmount,
    string Currency,
    TransactionStatus Status,
    DateTime InitiatedAt,
    DateTime? CompletedAt);

public record SubscriptionReportItemDto(
    Guid SubscriptionId,
    string CustomerName,
    string PhoneNumber,
    string PhoneOperator,
    string PartnerCode,
    SubscriptionStatus Status,
    DateTime SubscribedAt);

public record FailureAnalysisItemDto(
    string FailureReason,
    int Count,
    decimal TotalAmount);

public record AccountingReportItemDto(
    string AccountCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance);

public record PartnerStatementItemDto(
    Guid MovementId,
    DateTime MovementDate,
    MovementType MovementType,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Description,
    Guid? TransactionId);
