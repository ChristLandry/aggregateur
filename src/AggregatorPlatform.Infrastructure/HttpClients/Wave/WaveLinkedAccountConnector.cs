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

    // ---------------------------------------------------------------------
    // Opérations non fournies par la façade Wave (501)
    // ---------------------------------------------------------------------

    public Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(FacadeNotImplementedMessage);

    public Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(FacadeNotImplementedMessage);

    public Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(FacadeNotImplementedMessage);

    public Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(FacadeNotImplementedMessage);

    public Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(FacadeNotImplementedMessage);

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
