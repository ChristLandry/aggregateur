using System.Net.Http.Json;
using System.Text.Json;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Implémentation de <see cref="IWaveConnectorClient"/> qui délègue kyc/link/unlink
/// à la façade externe via le HttpClient nommé <c>WaveConnector</c>.
///
/// L'URL de base est prise sur <see cref="Partner.BaseUrl"/> à chaque appel
/// (chaque partenaire pointe donc sur sa propre instance de façade). Le header
/// <c>X-Partner-Id</c> est ajouté par la DI ; on ne le loggue JAMAIS.
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


    private HttpClient CreateClient(Partner partner)
    {
        var client = _factory.CreateClient("PartnerWallet");
        client.BaseAddress = new Uri(partner.BaseUrl);
        client.DefaultRequestHeaders.Add("X-Partner-Id", partner.PartnerId.ToString());
        return client;
    }


    public async Task<WaveKycResult> GetKycAsync(
        Partner partner,
        string walletTemporalyCode,
        string? alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var url = BuildUrl(partner, "/api/partner/kyc");
        var body = new { alias, walletTemporalyCode, extras };
        using var response = await client.PostAsJsonAsync(url, body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "kyc", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveKycResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty KYC response body.", (int)response.StatusCode);
    }

    public async Task<WaveLinkResult> LinkAsync(
        Partner partner,
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

        var client = CreateClient(partner);
        var url = BuildUrl(partner, "/api/partner/link");
        var body = new { bankAccount, alias, extras = merged };
        using var response = await client.PostAsJsonAsync(url, body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "link", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveLinkResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty link response body.", (int)response.StatusCode);
    }

    public async Task<WaveUnlinkResult> UnlinkAsync(
        Partner partner,
        string bankAccount,
        string alias,
        IDictionary<string, object?>? extras,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var url = BuildUrl(partner, "/api/partner/unlink");
        var body = new { bankAccount, alias, extras };
        using var response = await client.PostAsJsonAsync(url, body, JsonOpts, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "unlink", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<WaveUnlinkResult>(JsonOpts, cancellationToken);
        return result ?? throw new WaveConnectorException("Empty unlink response body.", (int)response.StatusCode);
    }

    /// <summary>
    /// Construit une URL absolue à partir de <see cref="Partner.BaseUrl"/> + le path de l'endpoint.
    /// Une URL absolue court-circuite le <c>BaseAddress</c> du HttpClient (qui n'est pas configuré ici).
    /// </summary>
    private static string BuildUrl(Partner partner, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(partner.BaseUrl))
            throw new WaveConnectorException(
                $"Partner '{partner.PartnerCode}' n'a pas de BaseUrl : configurer Partner.BaseUrl pour pointer sur la façade Wave.",
                400);

        var baseUrl = partner.BaseUrl.TrimEnd('/');
        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return baseUrl + path;
    }

    private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string op, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Wave connector {Op} failed: HTTP {Status} - {Body}",
            op, (int)response.StatusCode, body);

        var detail = string.IsNullOrWhiteSpace(body) ? string.Empty : $" Details: {Truncate(body, 400)}";
        throw new WaveConnectorException(
            $"Wave connector '{op}' failed with HTTP {(int)response.StatusCode}.{detail}",
            (int)response.StatusCode,
            body);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
