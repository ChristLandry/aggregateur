using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Connecteur wallet generique — MODE MOCK.
/// Toutes les operations renvoient une reponse SUCCESS deterministe, sans appel
/// HTTP sortant. Le OnboardCustomerCommandHandler enchaine ensuite normalement
/// la persistance Client + Customer + Subscription. Les logs [MOCK Wallet]
/// tracent chaque appel. Retirer les stubs et restaurer les appels HttpClient
/// une fois les partenaires wallet reels disponibles.
/// </summary>
public class WalletApiClient : IWalletApiClient
{
    private const string MockLinkIdPrefix = "mock-wallet-link-";
    private const string MockNationalId = "MOCK-SN-1990-0001";
    private const string MockFullName = "Mock Wallet Customer";
    private static readonly DateOnly MockDateOfBirth = new(1990, 1, 1);

    private readonly ILogger<WalletApiClient> _logger;

    // IHttpClientFactory reste injecte pour ne pas casser la DI, non utilise ici.
    public WalletApiClient(IHttpClientFactory factory, ILogger<WalletApiClient> logger)
    {
        _logger = logger;
        _ = factory;
    }

    public Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] GetBalance {PartnerId} phone={Phone}", partner.PartnerId, phoneNumber);
        return Task.FromResult(new WalletBalanceResponse(phoneNumber, 10_000m, partner.Currency, "ACTIVE"));
    }

    public Task<WalletKycDto> GetKycAsync(Partner partner, WalletKycRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] GetKyc {PartnerId} phone={Phone}", partner.PartnerId, request.PhoneNumber);
        return Task.FromResult(new WalletKycDto(
            PhoneNumber: request.PhoneNumber,
            FullName: MockFullName,
            DateOfBirth: MockDateOfBirth,
            NationalId: MockNationalId));
    }

    public Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] Debit {PartnerId} ref={Ref} amount={Amount}", partner.PartnerId, request.PartnerRef, request.Amount);
        return Task.FromResult(new WalletTransactionResponse(
            ExternalRef: MockLinkIdPrefix + Guid.NewGuid().ToString("N")[..8],
            Status: "SUCCESS",
            FailureReason: null));
    }

    public Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] Credit {PartnerId} ref={Ref} amount={Amount}", partner.PartnerId, request.PartnerRef, request.Amount);
        return Task.FromResult(new WalletTransactionResponse(
            ExternalRef: MockLinkIdPrefix + Guid.NewGuid().ToString("N")[..8],
            Status: "SUCCESS",
            FailureReason: null));
    }

    public Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] Cancel {PartnerId} ref={Ref}", partner.PartnerId, externalRef);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "SUCCESS", null));
    }

    public Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Wallet] GetStatus {PartnerId} ref={Ref}", partner.PartnerId, externalRef);
        return Task.FromResult(new WalletTransactionResponse(externalRef, "SUCCESS", null));
    }

    public Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default)
    {
        var linkId = MockLinkIdPrefix + Guid.NewGuid().ToString("N")[..12];
        _logger.LogInformation("[MOCK Wallet] Link {PartnerId} phone={Phone} bank={Bank} -> {LinkId}",
            partner.PartnerId, request.PhoneNumber, request.BankAccount, linkId);
        return Task.FromResult(new WalletLinkResponse(
            LinkId: linkId,
            PhoneNumber: request.PhoneNumber,
            Status: "SUCCESS",
            FailureReason: null));
    }

    public Task<WalletLinkResponse> UnlinkAsync(Partner partner, WalletUnlinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LinkId) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return Task.FromResult(new WalletLinkResponse(null, string.Empty, "FAILED",
                "LinkId or PhoneNumber must be provided."));
        }

        _logger.LogInformation("[MOCK Wallet] Unlink {PartnerId} linkId={LinkId} phone={Phone}",
            partner.PartnerId, request.LinkId, request.PhoneNumber);
        return Task.FromResult(new WalletLinkResponse(
            LinkId: request.LinkId ?? MockLinkIdPrefix + "unlinked",
            PhoneNumber: request.PhoneNumber ?? string.Empty,
            Status: "SUCCESS",
            FailureReason: null));
    }
}
