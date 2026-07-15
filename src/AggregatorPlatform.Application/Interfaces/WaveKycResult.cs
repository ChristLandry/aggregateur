namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Résultat de POST /api/partner/kyc.</summary>
/// <remarks>Attention : le champ <c>FullNam</c> conserve la typo volontaire du contrat.</remarks>
public record WaveKycResult(
    string IdNumber,
    string? IdType,
    string? FullNam,
    string? BirthDate,
    string? Status,
    object? Extra);
