namespace AggregatorPlatform.Application.Interfaces;

public record WalletTransactionRequest(string PartnerRef, string PhoneNumber, decimal Amount, string Currency, string? Description);
