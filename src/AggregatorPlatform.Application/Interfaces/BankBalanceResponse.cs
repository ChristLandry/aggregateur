namespace AggregatorPlatform.Application.Interfaces;

public record BankBalanceResponse(string PhoneNumber, decimal Balance, string Currency, string Status);
