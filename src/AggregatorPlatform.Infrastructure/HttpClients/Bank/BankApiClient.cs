using System.Net.Http.Json;
using System.Text.Json;
using AggregatorPlatform.Application.DTOs;
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

    public async Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        // Catch-all : couvre HttpRequestException, JsonException, mais aussi
        // Polly.CircuitBreaker.BrokenCircuitException quand le circuit est ouvert.
        // On veut toujours rendre une reponse structuree (status=UNKNOWN) plutot que 500.
        try
        {
            var client = CreateClient(partner);
            var resp = await client.GetFromJsonAsync<BankBalanceResponse>($"/bank/balance?phone={Uri.EscapeDataString(phoneNumber)}", cancellationToken);
            return resp ?? new BankBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank balance failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new BankBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
        }
    }

    public async Task<BankKycDto> GetKycAsync(Partner partner, BankKycRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(partner);
        try
        {
            var response = await client.PostAsJsonAsync("/bank/kyc", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<BankKycDto>(cancellationToken: cancellationToken);
            return body ?? new BankKycDto(request.PhoneNumber, string.Empty, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank KYC failed for partner {PartnerId} phone {Phone}", partner.PartnerId, request.PhoneNumber);
            return new BankKycDto(request.PhoneNumber, string.Empty, null, null);
        }
    }

    public async Task<BankTransactionResponse> DebitAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/debit", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(cancellationToken: cancellationToken);
            return body ?? new BankTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Bank debit JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new BankTransactionResponse(string.Empty, "FAILED", "Invalid JSON response from bank API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bank debit HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new BankTransactionResponse(string.Empty, "FAILED", $"Bank API communication failed: {ex.Message}");
        }
    }

    public async Task<BankTransactionResponse> CreditAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/credit", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(cancellationToken: cancellationToken);
            return body ?? new BankTransactionResponse(string.Empty, response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Bank credit JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new BankTransactionResponse(string.Empty, "FAILED", "Invalid JSON response from bank API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bank credit HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new BankTransactionResponse(string.Empty, "FAILED", $"Bank API communication failed: {ex.Message}");
        }
    }

    public async Task<BankTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var resp = await client.GetFromJsonAsync<BankTransactionResponse>($"/bank/status?ref={Uri.EscapeDataString(externalRef)}", cancellationToken);
            return resp ?? new BankTransactionResponse(externalRef, "UNKNOWN", null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Bank status JSON parsing failed for partner {PartnerId}, ref {Ref}", partner.PartnerId, externalRef);
            return new BankTransactionResponse(externalRef, "UNKNOWN", "Invalid JSON response from bank API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bank status HTTP request failed for partner {PartnerId}, ref {Ref}", partner.PartnerId, externalRef);
            return new BankTransactionResponse(externalRef, "UNKNOWN", $"Bank API communication failed: {ex.Message}");
        }
    }
}
