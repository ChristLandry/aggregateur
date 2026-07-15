namespace AggregatorPlatform.Application.Interfaces;

public record BankTransactionResponse(string ExternalRef, string Status, string? FailureReason);
