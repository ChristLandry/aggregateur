namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Résultat de POST /api/partner/unlink.</summary>
public record WaveUnlinkResult(
    bool Success,
    string BankAccount,
    string PartnerAccountId,
    string LinkRequestId,
    object? Extras);
