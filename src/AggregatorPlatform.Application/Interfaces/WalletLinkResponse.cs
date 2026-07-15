namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Reponse symetrique pour Link et Unlink.</summary>
public record WalletLinkResponse(
    string? LinkId,
    string PhoneNumber,
    string Status,
    string? FailureReason,
    DateTime? ExpiresAt = null);
