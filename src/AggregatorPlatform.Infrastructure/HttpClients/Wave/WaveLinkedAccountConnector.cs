using System.Globalization;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Connecteur exposé au reste de la codebase pour le partenaire <c>WAVE</c>.
/// - kyc / link / unlink : délèguent à la façade externe via <see cref="IWaveConnectorClient"/>.
/// - balance / debit / credit / cancel / status : la façade répond 501 (non implémenté).
///   Ces méthodes lèvent <see cref="NotImplementedException"/> tant que la façade
///   n'expose pas les endpoints correspondants (voir doc /api/wave/bank/*).
/// </summary>
public class WaveLinkedAccountConnector : IWalletApiClient
{
    private const string FacadeNotImplementedMessage =
        "La façade Wave ne fournit pas cet endpoint (réponse 501). " +
        "Utiliser un connecteur wallet spécifique ou implémenter l'endpoint côté façade.";

    private readonly IWaveConnectorClient _wave;
    private readonly ILogger<WaveLinkedAccountConnector> _logger;

    public WaveLinkedAccountConnector(
        IWaveConnectorClient wave,
        ILogger<WaveLinkedAccountConnector> logger)
    {
        _wave = wave;
        _logger = logger;
    }

    // ---------------------------------------------------------------------
    // KYC / Link / Unlink : délégués à la façade externe
    // ---------------------------------------------------------------------

    public async Task<WalletKycDto> GetKycAsync(Partner partner, WalletKycRequest request, CancellationToken cancellationToken = default)
    {
        var otp = request.PartnerTemporalyCode?.Trim();
        if (string.IsNullOrWhiteSpace(otp))
        {
            throw new WaveConnectorException(
                "Wave KYC requires walletTemporalyCode (OTP).",
                400);
        }

        try
        {
            var kyc = await _wave.GetKycAsync(
                partner,
                walletTemporalyCode: otp,
                alias: request.PhoneNumber.Trim(),
                extras: request.Extras,
                cancellationToken);

            var dob = DateOnly.TryParseExact(
                kyc.BirthDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsed) ? parsed : default;

            return new WalletKycDto(
                PhoneNumber: request.PhoneNumber,
                FullName: kyc.FullNam ?? string.Empty,
                DateOfBirth: dob,
                NationalId: kyc.IdNumber);
        }
        catch (Exception ex)
        {
            // Facade Wave indisponible / circuit ouvert / erreur reseau : on renvoie
            // un KYC vide plutot que de bulle en 500.
            _logger.LogError(ex, "Wave KYC failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new WalletKycDto(request.PhoneNumber, string.Empty, default, null);
        }
    }

    public async Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default)
    {
        var activationKey = TryReadExtra(request.Extras, "activationKey")
                            ?? TryReadExtra(request.Extras, "walletTemporalyCode");
        if (string.IsNullOrWhiteSpace(activationKey))
        {
            throw new WaveConnectorException(
                "Wave link requires extras.activationKey (wallet OTP).",
                400);
        }

        try
        {
            var link = await _wave.LinkAsync(
                partner,
                bankAccount: request.BankAccount,
                alias: request.PartnerRef,
                activationKey: activationKey,
                extras: request.Extras,
                cancellationToken);

            var failureReason = link.Success ? null : TryReadFailureReason(link.Extras);

            return new WalletLinkResponse(
                LinkId: link.Success ? link.PartnerAccountId : null,
                PhoneNumber: request.PhoneNumber,
                Status: link.Success ? "SUCCESS" : "FAILED",
                FailureReason: failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wave link failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new WalletLinkResponse(
                LinkId: null,
                PhoneNumber: request.PhoneNumber,
                Status: "FAILED",
                FailureReason: $"Wave facade unreachable: {ex.Message}");
        }
    }

    public async Task<WalletLinkResponse> UnlinkAsync(Partner partner, WalletUnlinkRequest request, CancellationToken cancellationToken = default)
    {
        var alias = !string.IsNullOrWhiteSpace(request.PartnerRef)
            ? request.PartnerRef
            : request.LinkId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(alias))
        {
            return new WalletLinkResponse(
                LinkId: null,
                PhoneNumber: request.PhoneNumber ?? string.Empty,
                Status: "FAILED",
                FailureReason: "Wave unlink requires PartnerRef (bank_link_reference) or LinkId.");
        }

        try
        {
            var result = await _wave.UnlinkAsync(
                partner,
                bankAccount: string.Empty,
                alias: alias,
                extras: null,
                cancellationToken);

            var failureReason = result.Success ? null : TryReadFailureReason(result.Extras);

            return new WalletLinkResponse(
                LinkId: request.LinkId,
                PhoneNumber: request.PhoneNumber ?? string.Empty,
                Status: result.Success ? "SUCCESS" : "FAILED",
                FailureReason: failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wave unlink failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new WalletLinkResponse(
                LinkId: request.LinkId,
                PhoneNumber: request.PhoneNumber ?? string.Empty,
                Status: "FAILED",
                FailureReason: $"Wave facade unreachable: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------
    // Opérations non fournies par la façade Wave (501)
    // ---------------------------------------------------------------------

    // La facade Wave (Aggregator.WaveConnector.Api) n'expose pas encore ces routes :
    // on renvoie des reponses structurees marquees NOT_IMPLEMENTED plutot que
    // de lever une exception -> les endpoints /wallet/balance & /wallet/status
    // repondent 200 avec status explicite, /wallet/debit|credit|cancel restent
    // interceptes par le handler et marques Failed.

    public Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Wave connector: GetBalance non implemente cote facade pour partner {PartnerId}.", partner.PartnerId);
        return Task.FromResult(new WalletBalanceResponse(phoneNumber, 0, partner.Currency, "NOT_IMPLEMENTED"));
    }

    public Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Wave connector: Debit non implemente cote facade pour partner {PartnerId}.", partner.PartnerId);
        return Task.FromResult(new WalletTransactionResponse(string.Empty, "FAILED", FacadeNotImplementedMessage));
    }

    public Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Wave connector: Credit non implemente cote facade pour partner {PartnerId}.", partner.PartnerId);
        return Task.FromResult(new WalletTransactionResponse(string.Empty, "FAILED", FacadeNotImplementedMessage));
    }

    public Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Wave connector: Cancel non implemente cote facade pour partner {PartnerId}.", partner.PartnerId);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "FAILED", FacadeNotImplementedMessage));
    }

    public Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Wave connector: GetStatus non implemente cote facade pour partner {PartnerId}.", partner.PartnerId);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "NOT_IMPLEMENTED", FacadeNotImplementedMessage));
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static string? TryReadExtra(Dictionary<string, object?>? extras, string key)
    {
        if (extras is null || !extras.TryGetValue(key, out var v)) return null;
        return v?.ToString();
    }

    private static string? TryReadFailureReason(object? extras)
    {
        if (extras is null) return null;
        if (extras is string s) return s;
        if (extras is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (je.TryGetProperty("failureReason", out var reason) && reason.ValueKind == System.Text.Json.JsonValueKind.String)
                return reason.GetString();
        }
        return extras.ToString();
    }
}
