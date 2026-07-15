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
