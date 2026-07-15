namespace AggregatorPlatform.Application.Interfaces;

public record BankTransactionRequest(string PartnerRef, string AccountNumber, decimal Amount, string Currency, string? Description);
