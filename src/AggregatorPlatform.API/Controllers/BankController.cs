using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Financial.Commands;
using AggregatorPlatform.Application.Features.Financial.Queries;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

/// <summary>
/// Endpoints bancaires cote partenaire (X-Partner-ApiKey requis).
/// Remplace la partie /api/v1/financial/bank/* de l'ancien FinancialController.
/// </summary>
[Route("api/v1/bank")]
[RequirePartner]
public class BankController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;

    public BankController(ICurrentPartnerService currentPartner) => _currentPartner = currentPartner;

    private Guid PartnerId => _currentPartner.PartnerId!.Value;

    /// <summary>Solde du compte bancaire d'un abonne.</summary>
    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<BalanceDto>>> GetBalance([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetBankBalanceQuery(PartnerId, subscriptionId), ct));

    /// <summary>KYC bank par numero de compte. Retourne l'identite du client rattache au compte.</summary>
    [HttpPost("kyc")]
    public async Task<ActionResult<ApiResponse<BankKycDto>>> GetKyc([FromBody] BankKycRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetBankKycQuery(PartnerId, request), ct));

    /// <summary>Initie un debit bancaire.</summary>
    [HttpPost("debit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Debit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new BankDebitCommand(PartnerId, request), ct));

    /// <summary>Initie un credit bancaire.</summary>
    [HttpPost("credit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Credit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new BankCreditCommand(PartnerId, request), ct));
}
