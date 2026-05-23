using System.Net.Http.Json;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

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
        var resp = await client.GetFromJsonAsync<WalletBalanceResponse>($"/wallet/balance?phone={Uri.EscapeDataString(phoneNumber)}", cancellationToken);
        return resp ?? new WalletBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
    }

    public async Task<WalletKycResponse> GetKycAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<WalletKycResponse>($"/wallet/kyc?phone={Uri.EscapeDataString(phoneNumber)}", cancellationToken);
        return resp ?? new WalletKycResponse(phoneNumber, string.Empty, "UNKNOWN", "NONE");
    }

    public async Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/debit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/credit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/wallet/cancel", new { externalRef }, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new WalletTransactionResponse(externalRef, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<WalletTransactionResponse>($"/wallet/status?ref={Uri.EscapeDataString(externalRef)}", cancellationToken);
        return resp ?? new WalletTransactionResponse(externalRef, "UNKNOWN", null);
    }
}
