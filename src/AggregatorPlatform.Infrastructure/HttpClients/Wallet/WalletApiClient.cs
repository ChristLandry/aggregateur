using System.Net.Http.Json;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Connecteur wallet generique. Appelle les endpoints REST du partenaire wallet
/// via le HttpClient nomme "PartnerWallet" (retry + circuit breaker Polly configures
/// dans <see cref="DependencyInjection"/>). Le header <c>X-Partner-Id</c> est ajoute
/// pour identifier le partenaire cote back du fournisseur.
/// </summary>
public class WalletApiClient : IWalletApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WalletApiClient> _logger;

    public WalletApiClient(IHttpClientFactory factory, ILogger<WalletApiClient> logger)
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

    public async Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<WalletBalanceResponse>(
            $"/wallet/balance?phone={Uri.EscapeDataString(phoneNumber)}", cancellationToken);
        return resp ?? new WalletBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
    }

    public async Task<WalletKycDto> GetKycAsync(Partner partner, WalletKycRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/kyc", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<WalletKycDto>(cancellationToken: cancellationToken);
        return body ?? throw new InvalidOperationException("Empty KYC response body.");
    }

    public async Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/debit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(string.Empty,
            response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/credit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(string.Empty,
            response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/cancel", new { externalRef }, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(externalRef,
            response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<WalletTransactionResponse>(
            $"/wallet/status?ref={Uri.EscapeDataString(externalRef)}", cancellationToken);
        return resp ?? new WalletTransactionResponse(externalRef, "UNKNOWN", null);
    }

    public async Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/link", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletLinkResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletLinkResponse(
            LinkId: null,
            PhoneNumber: request.PhoneNumber,
            Status: response.IsSuccessStatusCode ? "SUCCESS" : "FAILED",
            FailureReason: response.IsSuccessStatusCode ? null : response.ReasonPhrase);
    }

    public async Task<WalletLinkResponse> UnlinkAsync(Partner partner, WalletUnlinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LinkId) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return new WalletLinkResponse(null, string.Empty, "FAILED",
                "LinkId or PhoneNumber must be provided.");
        }

        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/unlink", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletLinkResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletLinkResponse(
            LinkId: request.LinkId,
            PhoneNumber: request.PhoneNumber ?? string.Empty,
            Status: response.IsSuccessStatusCode ? "SUCCESS" : "FAILED",
            FailureReason: response.IsSuccessStatusCode ? null : response.ReasonPhrase);
    }
}
