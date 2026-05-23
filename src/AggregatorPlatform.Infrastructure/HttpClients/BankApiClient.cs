using System.Net.Http.Json;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

public class BankApiClient : IBankApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<BankApiClient> _logger;

    public BankApiClient(IHttpClientFactory factory, ILogger<BankApiClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    private HttpClient CreateClient(Partner partner)
    {
        var client = _factory.CreateClient("PartnerBank");
        client.BaseAddress = new Uri(partner.BaseUrl);
        client.DefaultRequestHeaders.Add("X-Partner-Id", partner.PartnerId.ToString());
        return client;
    }

    public async Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string accountNumber, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<BankBalanceResponse>($"/bank/balance?account={Uri.EscapeDataString(accountNumber)}", cancellationToken);
        return resp ?? new BankBalanceResponse(accountNumber, 0, partner.Currency, "UNKNOWN");
    }

    public async Task<BankKycResponse> GetKycAsync(Partner partner, string accountNumber, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<BankKycResponse>($"/bank/kyc?account={Uri.EscapeDataString(accountNumber)}", cancellationToken);
        return resp ?? new BankKycResponse(accountNumber, string.Empty, "UNKNOWN", "NONE");
    }

    public async Task<BankTransactionResponse> DebitAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/bank/debit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new BankTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<BankTransactionResponse> CreditAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var response = await client.PostAsJsonAsync("/bank/credit", request, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(cancellationToken: cancellationToken);
        return body ?? new BankTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
    }

    public async Task<BankTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        var resp = await client.GetFromJsonAsync<BankTransactionResponse>($"/bank/status?ref={Uri.EscapeDataString(externalRef)}", cancellationToken);
        return resp ?? new BankTransactionResponse(externalRef, "UNKNOWN", null);
    }
}
