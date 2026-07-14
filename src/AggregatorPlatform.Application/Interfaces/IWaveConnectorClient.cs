using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Client HTTP typé qui délègue les opérations Wave à la façade externe
/// (Aggregator.WaveConnector.Api). Le backend ne parle jamais directement à
/// sn.mmapp.wave.com — il passe uniquement par cette interface.
///
/// L'URL de base est construite à partir de <see cref="Partner.BaseUrl"/> passé
/// à chaque appel : chaque partenaire pointe donc sur sa propre instance de façade.
/// </summary>
public interface IWaveConnectorClient
{
    Task<WaveKycResult> GetKycAsync(
        Partner partner,
        string walletTemporalyCode,
        string? alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default);

    Task<WaveLinkResult> LinkAsync(
        Partner partner,
        string bankAccount,
        string alias,
        string activationKey,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default);

    Task<WaveUnlinkResult> UnlinkAsync(
        Partner partner,
        string bankAccount,
        string alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default);
}

/// <summary>Résultat de POST /api/wave/linked_account/kyc.</summary>
/// <remarks>Attention : le champ <c>FullNam</c> conserve la typo volontaire du contrat.</remarks>
public record WaveKycResult(
    string IdNumber,
    string? IdType,
    string? FullNam,
    string? BirthDate,
    string? Status,
    object? Extra);

/// <summary>Résultat de POST /api/wave/linked_account/link.</summary>
public record WaveLinkResult(
    bool Success,
    string BankAccount,
    string PartnerAccountId,
    string LinkRequestId,
    object? Extras);

/// <summary>Résultat de POST /api/wave/linked_account/unlink.</summary>
public record WaveUnlinkResult(
    bool Success,
    string BankAccount,
    string PartnerAccountId,
    string LinkRequestId,
    object? Extras);

/// <summary>
/// Levée quand la façade renvoie un code HTTP ≥ 400. Un <c>success:false</c>
/// (200 OK côté HTTP) reste un cas métier et n'est PAS traduit en exception.
/// </summary>
public class WaveConnectorException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public WaveConnectorException(string message, int statusCode, string? responseBody = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
