namespace AggregatorPlatform.Application.Interfaces;

public record BankBalanceResponse(string AccountNumber, decimal Balance, string Currency, string Status);
