namespace AggregatorPlatform.Application.Interfaces;

public record WalletTransactionResponse(string ExternalRef, string Status, string? FailureReason);
