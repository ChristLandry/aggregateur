using System.Globalization;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Connecteur exposé au reste de la codebase pour le partenaire <c>WAVE</c>.
/// - kyc / link / unlink : délèguent à la façade externe via <see cref="IWaveConnectorClient"/>.
/// - balance / debit / credit / cancel / status : la façade répond 501 → on garde
///   un mock déterministe pour ne pas casser les autres flux tant que l'implémentation
///   générique n'est pas branchée.
/// </summary>
public class WaveLinkedAccountConnector : IWalletApiClient
{
    private const string MockPrefix = "mock-wave-";
    private const string WaveActiveStatus = "active";

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
        // Contrat façade : { alias, walletTemporalyCode, extras }.
        // Alias = PartnerCode par défaut (le back sait à quel partenaire il parle).
        var kyc = await _wave.GetKycAsync(
            walletTemporalyCode: request.PartnerTemporalyCode ?? request.PhoneNumber,
            alias: partner.PartnerCode,
            extras: request.Extras,
            cancellationToken);

        var dob = DateOnly.TryParseExact(
            kyc.BirthDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var parsed) ? parsed : default;

        // La façade ne renvoie pas le numéro de téléphone → on echo l'entrée.
        return new WalletKycDto(
            PhoneNumber: request.PhoneNumber,
            FullName: kyc.FullNam ?? string.Empty,
            DateOfBirth: dob,
            NationalId: kyc.IdNumber);
    }

    public async Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default)
    {
        // Contrat façade : { bankAccount, alias, extras.activationKey (obligatoire) }.
        // Alias = PartnerRef côté client (utilisé aussi pour /unlink).
        // activationKey = PartnerTemporalyCode fourni côté client, sinon PhoneNumber.
        var activationKey = TryReadExtra(request.Extras, "activationKey")
                            ?? request.PhoneNumber;

        var link = await _wave.LinkAsync(
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
        // Contrat façade unlink : alias = bank_link_reference utilisée lors du /link.
        // On mappe : PartnerRef → alias (préféré), sinon LinkId comme repli.
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

        // bankAccount n'est pas transporté dans WalletUnlinkRequest — la façade
        // le tolère vide car elle route sur l'alias.
        var result = await _wave.UnlinkAsync(
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
    // Opérations que la façade renvoie en 501 → mock local
    // ---------------------------------------------------------------------

    public Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wave] GetBalance {PartnerId}", partner.PartnerId);
        return Task.FromResult(new WalletBalanceResponse(phoneNumber, 10_000m, partner.Currency, "ACTIVE"));
    }

    public Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wave] Debit {PartnerId} ref={Ref}", partner.PartnerId, request.PartnerRef);
        return Task.FromResult(new WalletTransactionResponse(MockPrefix + Guid.NewGuid().ToString("N")[..8], "SUCCESS", null));
    }

    public Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wave] Credit {PartnerId} ref={Ref}", partner.PartnerId, request.PartnerRef);
        return Task.FromResult(new WalletTransactionResponse(MockPrefix + Guid.NewGuid().ToString("N")[..8], "SUCCESS", null));
    }

    public Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wave] Cancel {PartnerId} ref={Ref}", partner.PartnerId, externalRef);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "SUCCESS", null));
    }

    public Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wave] GetStatus {PartnerId} ref={Ref}", partner.PartnerId, externalRef);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "SUCCESS", null));
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static string? TryReadExtra(Dictionary<string, object?>? extras, string key)
    {
        if (extras is null || !extras.TryGetValue(key, out var v)) return null;
        return v?.ToString();
    }

    /// <summary>
    /// La façade renvoie <c>extras.failureReason</c> sous forme d'objet ou de string
    /// quand <c>success=false</c>. On tente une extraction souple sans exploser sur les types.
    /// </summary>
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
