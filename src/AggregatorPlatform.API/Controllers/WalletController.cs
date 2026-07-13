using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Financial.Commands;
using AggregatorPlatform.Application.Features.Financial.Queries;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

/// <summary>
/// Endpoints wallet cote partenaire (X-Partner-ApiKey requis).
/// Tous les appels sortants passent par <see cref="IWalletConnectorResolver"/>
/// qui choisit le bon connecteur (Wave, generique, ...) a partir du PartnerCode.
/// </summary>
[Route("api/v1/wallet")]
[RequirePartner]
public class WalletController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;
    private readonly IWalletConnectorResolver _resolver;

    public WalletController(ICurrentPartnerService currentPartner, IWalletConnectorResolver resolver)
    {
        _currentPartner = currentPartner;
        _resolver = resolver;
    }

    private Guid PartnerId => _currentPartner.PartnerId!.Value;

    /// <summary>Solde du wallet d'un abonne.</summary>
    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<BalanceDto>>> GetBalance([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetWalletBalanceQuery(PartnerId, subscriptionId), ct));

    /// <summary>KYC wallet par numero de telephone. Retourne l'identite du client rattache au wallet.</summary>
    [HttpPost("kyc")]
    public async Task<ActionResult<ApiResponse<WalletKycDto>>> GetKyc([FromBody] WalletKycRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetWalletKycQuery(PartnerId, request), ct));

    /// <summary>Initie un debit wallet.</summary>
    [HttpPost("debit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Debit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletDebitCommand(PartnerId, request), ct));

    /// <summary>Initie un credit wallet.</summary>
    [HttpPost("credit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Credit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletCreditCommand(PartnerId, request), ct));

    /// <summary>Annule une transaction wallet.</summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Cancel([FromBody] CancelTransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletCancelCommand(PartnerId, request), ct));

    /// <summary>
    /// Lie un compte bancaire a un wallet. Le connecteur est resolu a partir du PartnerCode.
    /// Reponse a plat (WalletOperationEnvelope), non enveloppee dans ApiResponse.
    /// </summary>
    [HttpPost("link")]
    public async Task<ActionResult<WalletOperationEnvelope>> Link([FromBody] WalletLinkRequest request, CancellationToken ct)
    {
        var partner = _currentPartner.Current!;
        var connector = _resolver.Resolve(partner);
        var partnerResult = await connector.LinkAsync(partner, request, ct);
        var success = string.Equals(partnerResult.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

        var envelope = new WalletOperationEnvelope(
            Success: success,
            TransactionId: null,
            PartnerTransactionRef: request.PartnerRef,
            Status: partnerResult.Status,
            Data: success
                ? new { linkId = partnerResult.LinkId, expiresAt = partnerResult.ExpiresAt }
                : null,
            ErrorCode: success ? null : "WALLET_LINK_FAILED",
            ErrorMessage: success ? null : partnerResult.FailureReason,
            Timestamp: DateTime.UtcNow);

        return Ok(envelope);
    }

    /// <summary>
    /// Delie un wallet (par LinkId ou PhoneNumber). Le connecteur est resolu a partir du PartnerCode.
    /// </summary>
    [HttpPost("unlink")]
    public async Task<ActionResult<ApiResponse<WalletLinkResponse>>> Unlink([FromBody] WalletUnlinkRequest request, CancellationToken ct)
    {
        var partner = _currentPartner.Current!;
        var connector = _resolver.Resolve(partner);
        var resp = await connector.UnlinkAsync(partner, request, ct);
        return Ok(ApiResponse<WalletLinkResponse>.Ok(resp));
    }
}
