namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Résultat de POST /api/partner/link.</summary>
public record WaveLinkResult(
    bool Success,
    string BankAccount,
    string PartnerAccountId,
    string LinkRequestId,
    object? Extras);
