namespace AggregatorPlatform.Application.Interfaces;

public record WalletBalanceResponse(string PhoneNumber, decimal Balance, string Currency, string Status);
