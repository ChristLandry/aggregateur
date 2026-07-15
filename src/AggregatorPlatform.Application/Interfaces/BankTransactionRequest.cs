namespace AggregatorPlatform.Application.Interfaces;

public record BankTransactionRequest(string PartnerRef, string PhoneNumber, decimal Amount, string Currency, string? Description);
