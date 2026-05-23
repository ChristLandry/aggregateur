using System.Globalization;
using System.Text;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Reports.Queries;
using AggregatorPlatform.Domain.Enums;
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/reports")]
[Authorize]
public class ReportController : BaseApiController
{
    /// <summary>Transactions report.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransactionReportItemDto>>>> Transactions(
        [FromQuery] Guid? partnerId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] TransactionStatus? status, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetTransactionReportQuery(partnerId, fromDate, toDate, status), ct));

    /// <summary>Subscriptions report.</summary>
    [HttpGet("subscriptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionReportItemDto>>>> Subscriptions(
        [FromQuery] Guid? partnerId, [FromQuery] SubscriptionStatus? status, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetSubscriptionReportQuery(partnerId, status), ct));

    /// <summary>Failure analysis report.</summary>
    [HttpGet("failure-analysis")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FailureAnalysisItemDto>>>> FailureAnalysis(
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetFailureAnalysisQuery(fromDate, toDate), ct));

    /// <summary>Accounting report (debit/credit by account).</summary>
    [HttpGet("accounting")]
    [Authorize(Roles = "Admin,SuperAdmin,Finance")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountingReportItemDto>>>> Accounting(
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetAccountingReportQuery(fromDate, toDate), ct));

    /// <summary>Partner account statement.</summary>
    [HttpGet("partner-account-statement")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PartnerStatementItemDto>>>> PartnerStatement(
        [FromQuery] Guid partnerId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetPartnerStatementQuery(partnerId, fromDate, toDate), ct));

    /// <summary>Export a report (CSV or XLSX).</summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ExportReportRequest request, [FromServices] IMediator mediator, CancellationToken ct)
    {
        if (!string.Equals(request.ReportType, "transactions", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("UNSUPPORTED_REPORT", "Only 'transactions' export is supported in this stub."));

        var result = await mediator.Send(new GetTransactionReportQuery(request.PartnerId, request.FromDate, request.ToDate, null), ct);
        if (result.IsFailure) return BadRequest(ApiResponse.Fail(result.ErrorCode!, result.ErrorMessage!));
        var data = result.Value!;

        if (request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("TransactionId,PartnerRef,PartnerCode,Type,Amount,Fee,Currency,Status,InitiatedAt,CompletedAt");
            foreach (var t in data)
                sb.AppendLine(string.Join(',',
                    t.TransactionId, t.PartnerTransactionRef, t.PartnerCode, t.Type,
                    t.Amount.ToString(CultureInfo.InvariantCulture),
                    t.FeeAmount.ToString(CultureInfo.InvariantCulture),
                    t.Currency, t.Status, t.InitiatedAt.ToString("o"),
                    t.CompletedAt?.ToString("o") ?? ""));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "transactions.csv");
        }

        // XLSX
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Transactions");
        sheet.Cell(1, 1).Value = "TransactionId";
        sheet.Cell(1, 2).Value = "PartnerRef";
        sheet.Cell(1, 3).Value = "PartnerCode";
        sheet.Cell(1, 4).Value = "Type";
        sheet.Cell(1, 5).Value = "Amount";
        sheet.Cell(1, 6).Value = "Fee";
        sheet.Cell(1, 7).Value = "Currency";
        sheet.Cell(1, 8).Value = "Status";
        sheet.Cell(1, 9).Value = "InitiatedAt";
        sheet.Cell(1, 10).Value = "CompletedAt";

        int row = 2;
        foreach (var t in data)
        {
            sheet.Cell(row, 1).Value = t.TransactionId.ToString();
            sheet.Cell(row, 2).Value = t.PartnerTransactionRef;
            sheet.Cell(row, 3).Value = t.PartnerCode;
            sheet.Cell(row, 4).Value = t.Type.ToString();
            sheet.Cell(row, 5).Value = t.Amount;
            sheet.Cell(row, 6).Value = t.FeeAmount;
            sheet.Cell(row, 7).Value = t.Currency;
            sheet.Cell(row, 8).Value = t.Status.ToString();
            sheet.Cell(row, 9).Value = t.InitiatedAt;
            sheet.Cell(row, 10).Value = t.CompletedAt;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "transactions.xlsx");
    }
}

public record ExportReportRequest(string ReportType, string Format, Guid? PartnerId, DateTime? FromDate, DateTime? ToDate);
