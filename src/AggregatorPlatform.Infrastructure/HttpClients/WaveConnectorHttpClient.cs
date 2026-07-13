using System.Net.Http.Json;
using System.Text.Json;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Implémentation de <see cref="IWaveConnectorClient"/> qui délègue kyc/link/unlink
/// à la façade externe via le HttpClient nommé <c>WaveConnector</c>.
/// Le header <c>Api-Key</c> est ajouté par la DI ; on ne le loggue JAMAIS.
/// </summary>
public class WaveConnectorHttpClient : IWaveConnectorClient
{
    public const string HttpClientName = "WaveConnector";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WaveConnectorHttpClient> _logger;

    public WaveConnectorHttpClient(IHttpClientFactory factory, ILogger<WaveConnectorHttpClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<WaveKycResult> GetKycAsync(
        string walletTemporalyCode,
        string? alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient(HttpClientName);
        var body = new { alias, walletTemporalyCode, extras };
        using var response = await client.PostAsJsonAsync("/api/wave/linked_account/kyc", body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "kyc", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveKycResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty KYC response body.", (int)response.StatusCode);
    }

    public async Task<WaveLinkResult> LinkAsync(
        string bankAccount,
        string alias,
        string activationKey,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default)
    {
        // La façade exige extras.activationKey ; on l'ajoute/écrase s'il manque.
        var merged = extras is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(extras);
        merged["activationKey"] = activationKey;

        var client = _factory.CreateClient(HttpClientName);
        var body = new { bankAccount, alias, extras = merged };
        using var response = await client.PostAsJsonAsync("/api/wave/linked_account/link", body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "link", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveLinkResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty link response body.", (int)response.StatusCode);
    }

    public async Task<WaveUnlinkResult> UnlinkAsync(
        string bankAccount,
        string alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient(HttpClientName);
        var body = new { bankAccount, alias, extras };
        using var response = await client.PostAsJsonAsync("/api/wave/linked_account/unlink", body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "unlink", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveUnlinkResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty unlink response body.", (int)response.StatusCode);
    }

    private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string op, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        // On ne logue PAS le header Api-Key (jamais visible ici) — uniquement l'op + statut + body de réponse.
        _logger.LogWarning("Wave connector {Op} failed: HTTP {Status} - {Body}",
            op, (int)response.StatusCode, body);

        throw new WaveConnectorException(
            $"Wave connector '{op}' failed with HTTP {(int)response.StatusCode}.",
            (int)response.StatusCode,
            body);
    }
}
