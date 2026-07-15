using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IWalletApiClient
{
    Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default);
    Task<WalletKycDto> GetKycAsync(Partner partner, WalletKycRequest request, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lie un wallet client (numero de telephone) au partenaire pour autoriser
    /// les operations ulterieures. Le header X-Partner-Id du partenaire est
    /// envoye dans l'appel.
    /// </summary>
    Task<WalletLinkResponse> LinkAsync(Partner partner, WalletLinkRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Defait la liaison wallet (par phoneNumber ou linkId). Le header X-Partner-Id
    /// du partenaire est envoye dans l'appel.
    /// </summary>
    Task<WalletLinkResponse> UnlinkAsync(Partner partner, WalletUnlinkRequest request, CancellationToken cancellationToken = default);
}
