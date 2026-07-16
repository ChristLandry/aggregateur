using System.Net.Http.Json;
using System.Text.Json;
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
        // Catch-all : HttpRequestException, JsonException et Polly.BrokenCircuitException
        // couverts ; on renvoie toujours une reponse structuree (UNKNOWN) plutot que 500.
        try
        {
            var client = CreateClient(partner);
            var resp = await client.GetFromJsonAsync<WalletBalanceResponse>(
                $"/wallet/balance?phone={Uri.EscapeDataString(phoneNumber)}", cancellationToken);
            return resp ?? new WalletBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallet balance failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new WalletBalanceResponse(phoneNumber, 0, partner.Currency, "UNKNOWN");
        }
    }

    public async Task<WalletKycDto> GetKycAsync(Partner partner, WalletKycRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/wallet/kyc", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<WalletKycDto>(cancellationToken: cancellationToken);
            return body ?? new WalletKycDto(request.PhoneNumber, string.Empty, default, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallet KYC failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new WalletKycDto(request.PhoneNumber, string.Empty, default, null);
        }
    }

    public async Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/wallet/debit", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
            return body ?? new WalletTransactionResponse(string.Empty,
                response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet debit JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(string.Empty, "FAILED", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet debit HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(string.Empty, "FAILED", $"Wallet API communication failed: {ex.Message}");
        }
    }

    public async Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/wallet/credit", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
            return body ?? new WalletTransactionResponse(string.Empty,
                response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet credit JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(string.Empty, "FAILED", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet credit HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(string.Empty, "FAILED", $"Wallet API communication failed: {ex.Message}");
        }
    }

    public async Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/wallet/cancel", new { externalRef }, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<WalletTransactionResponse>(cancellationToken: cancellationToken);
            return body ?? new WalletTransactionResponse(externalRef,
                response.IsSuccessStatusCode ? "SUCCESS" : "FAILED", response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet cancel JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(externalRef, "FAILED", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet cancel HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new WalletTransactionResponse(externalRef, "FAILED", $"Wallet API communication failed: {ex.Message}");
        }
    }

    public async Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var resp = await client.GetFromJsonAsync<WalletTransactionResponse>(
                $"/wallet/status?ref={Uri.EscapeDataString(externalRef)}", cancellationToken);
            return resp ?? new WalletTransactionResponse(externalRef, "UNKNOWN", null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet status JSON parsing failed for partner {PartnerId}, ref {Ref}", partner.PartnerId, externalRef);
            return new WalletTransactionResponse(externalRef, "UNKNOWN", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet status HTTP request failed for partner {PartnerId}, ref {Ref}", partner.PartnerId, externalRef);
            return new WalletTransactionResponse(externalRef, "UNKNOWN", $"Wallet API communication failed: {ex.Message}");
        }
    }

    public async Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default)
    {
        try
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
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet link JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new WalletLinkResponse(null, request.PhoneNumber, "FAILED", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet link HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new WalletLinkResponse(null, request.PhoneNumber, "FAILED", $"Wallet API communication failed: {ex.Message}");
        }
    }

    public async Task<WalletLinkResponse> UnlinkAsync(Partner partner, WalletUnlinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LinkId) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return new WalletLinkResponse(null, string.Empty, "FAILED",
                "LinkId or PhoneNumber must be provided.");
        }

        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/wallet/unlink", request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<WalletLinkResponse>(cancellationToken: cancellationToken);
            return body ?? new WalletLinkResponse(
                LinkId: request.LinkId,
                PhoneNumber: request.PhoneNumber ?? string.Empty,
                Status: response.IsSuccessStatusCode ? "SUCCESS" : "FAILED",
                FailureReason: response.IsSuccessStatusCode ? null : response.ReasonPhrase);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Wallet unlink JSON parsing failed for partner {PartnerId}", partner.PartnerId);
            return new WalletLinkResponse(request.LinkId, request.PhoneNumber ?? string.Empty, "FAILED", "Invalid JSON response from wallet API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wallet unlink HTTP request failed for partner {PartnerId}", partner.PartnerId);
            return new WalletLinkResponse(request.LinkId, request.PhoneNumber ?? string.Empty, "FAILED", $"Wallet API communication failed: {ex.Message}");
        }
    }
}
