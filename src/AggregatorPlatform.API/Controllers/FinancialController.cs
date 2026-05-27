using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Financial.Commands;
using AggregatorPlatform.Application.Features.Financial.Queries;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/financial")]
[RequirePartner]
public class FinancialController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;

    public FinancialController(ICurrentPartnerService currentPartner) => _currentPartner = currentPartner;

    private Guid PartnerId => _currentPartner.PartnerId!.Value;

    /// <summary>Get bank account balance for a subscription.</summary>
    [HttpGet("bank/balance")]
    public async Task<ActionResult<ApiResponse<BalanceDto>>> GetBankBalance([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetBankBalanceQuery(PartnerId, subscriptionId), ct));

    /// <summary>Get wallet balance for a subscription.</summary>
    [HttpGet("wallet/balance")]
    public async Task<ActionResult<ApiResponse<BalanceDto>>> GetWalletBalance([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetWalletBalanceQuery(PartnerId, subscriptionId), ct));

    /// <summary>Get bank KYC info.</summary>
    [HttpGet("bank/kyc")]
    public async Task<ActionResult<ApiResponse<KycDto>>> GetBankKyc([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetBankKycQuery(PartnerId, subscriptionId), ct));

    /// <summary>Get wallet KYC info.</summary>
    [HttpGet("wallet/kyc")]
    public async Task<ActionResult<ApiResponse<KycDto>>> GetWalletKyc([FromQuery] Guid subscriptionId, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetWalletKycQuery(PartnerId, subscriptionId), ct));

    /// <summary>Initiate a bank debit transaction.</summary>
    [HttpPost("bank/debit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> BankDebit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new BankDebitCommand(PartnerId, request), ct));

    /// <summary>Initiate a bank credit transaction.</summary>
    [HttpPost("bank/credit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> BankCredit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new BankCreditCommand(PartnerId, request), ct));

    /// <summary>Initiate a wallet debit transaction.</summary>
    [HttpPost("wallet/debit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> WalletDebit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletDebitCommand(PartnerId, request), ct));

    /// <summary>Initiate a wallet credit transaction.</summary>
    [HttpPost("wallet/credit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> WalletCredit([FromBody] TransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletCreditCommand(PartnerId, request), ct));

    /// <summary>Cancel a wallet transaction.</summary>
    [HttpPost("wallet/cancel")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> WalletCancel([FromBody] CancelTransactionRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new WalletCancelCommand(PartnerId, request), ct));

    // Note : les lectures de transactions (search, detail, movements) sont desormais
    // exposees par TransactionsAdminController sous /api/v1/financial/transactions
    // (JWT admin, pas de header X-Partner-ApiKey).
}
