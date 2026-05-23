using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Reports.Queries;

public record GetTransactionReportQuery(Guid? PartnerId, DateTime? FromDate, DateTime? ToDate, TransactionStatus? Status)
    : IRequest<Result<IReadOnlyList<TransactionReportItemDto>>>;

public record GetSubscriptionReportQuery(Guid? PartnerId, SubscriptionStatus? Status)
    : IRequest<Result<IReadOnlyList<SubscriptionReportItemDto>>>;

public record GetFailureAnalysisQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<IReadOnlyList<FailureAnalysisItemDto>>>;

public record GetAccountingReportQuery(DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<IReadOnlyList<AccountingReportItemDto>>>;

public record GetPartnerStatementQuery(Guid PartnerId, DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<IReadOnlyList<PartnerStatementItemDto>>>;

public class GetTransactionReportQueryHandler : IRequestHandler<GetTransactionReportQuery, Result<IReadOnlyList<TransactionReportItemDto>>>
{
    private readonly ITransactionRepository _txs;

    public GetTransactionReportQueryHandler(ITransactionRepository txs) => _txs = txs;

    public async Task<Result<IReadOnlyList<TransactionReportItemDto>>> Handle(GetTransactionReportQuery request, CancellationToken cancellationToken)
    {
        var q = _txs.Query().Include(t => t.Partner).AsQueryable();
        if (request.PartnerId.HasValue) q = q.Where(t => t.PartnerId == request.PartnerId);
        if (request.FromDate.HasValue) q = q.Where(t => t.InitiatedAt >= request.FromDate);
        if (request.ToDate.HasValue) q = q.Where(t => t.InitiatedAt <= request.ToDate);
        if (request.Status.HasValue) q = q.Where(t => t.Status == request.Status);

        var list = await q.OrderByDescending(t => t.InitiatedAt)
            .Select(t => new TransactionReportItemDto(
                t.TransactionId, t.PartnerTransactionRef, t.Partner!.PartnerCode,
                t.TransactionType, t.Amount, t.FeeAmount, t.Currency, t.Status, t.InitiatedAt, t.CompletedAt))
            .Take(10000)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TransactionReportItemDto>>.Success(list);
    }
}

public class GetSubscriptionReportQueryHandler : IRequestHandler<GetSubscriptionReportQuery, Result<IReadOnlyList<SubscriptionReportItemDto>>>
{
    private readonly ISubscriptionRepository _subs;

    public GetSubscriptionReportQueryHandler(ISubscriptionRepository subs) => _subs = subs;

    public async Task<Result<IReadOnlyList<SubscriptionReportItemDto>>> Handle(GetSubscriptionReportQuery request, CancellationToken cancellationToken)
    {
        var q = _subs.Query().Include(s => s.Customer).Include(s => s.Partner).AsQueryable();
        if (request.PartnerId.HasValue) q = q.Where(s => s.PartnerId == request.PartnerId);
        if (request.Status.HasValue) q = q.Where(s => s.Status == request.Status);

        var list = await q.Select(s => new SubscriptionReportItemDto(
                s.SubscriptionId, s.Customer!.FullName, s.PhoneNumber, s.PhoneOperator,
                s.Partner!.PartnerCode, s.Status, s.SubscribedAt))
            .Take(10000)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SubscriptionReportItemDto>>.Success(list);
    }
}

public class GetFailureAnalysisQueryHandler : IRequestHandler<GetFailureAnalysisQuery, Result<IReadOnlyList<FailureAnalysisItemDto>>>
{
    private readonly ITransactionRepository _txs;

    public GetFailureAnalysisQueryHandler(ITransactionRepository txs) => _txs = txs;

    public async Task<Result<IReadOnlyList<FailureAnalysisItemDto>>> Handle(GetFailureAnalysisQuery request, CancellationToken cancellationToken)
    {
        var q = _txs.Query().Where(t => t.Status == TransactionStatus.Failed);
        if (request.FromDate.HasValue) q = q.Where(t => t.InitiatedAt >= request.FromDate);
        if (request.ToDate.HasValue) q = q.Where(t => t.InitiatedAt <= request.ToDate);

        var grouped = await q.GroupBy(t => t.FailureReason ?? "UNKNOWN")
            .Select(g => new FailureAnalysisItemDto(g.Key, g.Count(), g.Sum(t => t.Amount)))
            .OrderByDescending(g => g.Count)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FailureAnalysisItemDto>>.Success(grouped);
    }
}

public class GetAccountingReportQueryHandler : IRequestHandler<GetAccountingReportQuery, Result<IReadOnlyList<AccountingReportItemDto>>>
{
    private readonly IRepository<JournalLine> _lines;

    public GetAccountingReportQueryHandler(IRepository<JournalLine> lines) => _lines = lines;

    public async Task<Result<IReadOnlyList<AccountingReportItemDto>>> Handle(GetAccountingReportQuery request, CancellationToken cancellationToken)
    {
        var q = _lines.Query().Include(l => l.Entry).AsQueryable();
        if (request.FromDate.HasValue) q = q.Where(l => l.Entry!.EntryDate >= request.FromDate);
        if (request.ToDate.HasValue) q = q.Where(l => l.Entry!.EntryDate <= request.ToDate);

        var grouped = await q.GroupBy(l => l.AccountCode).Select(g => new AccountingReportItemDto(
            g.Key,
            g.Where(x => x.Side == LedgerSide.Debit).Sum(x => x.Amount),
            g.Where(x => x.Side == LedgerSide.Credit).Sum(x => x.Amount),
            g.Where(x => x.Side == LedgerSide.Debit).Sum(x => x.Amount) - g.Where(x => x.Side == LedgerSide.Credit).Sum(x => x.Amount)))
            .OrderBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AccountingReportItemDto>>.Success(grouped);
    }
}

public class GetPartnerStatementQueryHandler : IRequestHandler<GetPartnerStatementQuery, Result<IReadOnlyList<PartnerStatementItemDto>>>
{
    private readonly IRepository<PartnerAccountMovement> _movements;

    public GetPartnerStatementQueryHandler(IRepository<PartnerAccountMovement> movements) => _movements = movements;

    public async Task<Result<IReadOnlyList<PartnerStatementItemDto>>> Handle(GetPartnerStatementQuery request, CancellationToken cancellationToken)
    {
        var q = _movements.Query().Where(m => m.PartnerId == request.PartnerId);
        if (request.FromDate.HasValue) q = q.Where(m => m.MovementDate >= request.FromDate);
        if (request.ToDate.HasValue) q = q.Where(m => m.MovementDate <= request.ToDate);

        var list = await q.OrderByDescending(m => m.MovementDate)
            .Select(m => new PartnerStatementItemDto(
                m.MovementId, m.MovementDate, m.MovementType, m.Amount,
                m.BalanceBefore, m.BalanceAfter, m.Description, m.TransactionId))
            .Take(10000)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PartnerStatementItemDto>>.Success(list);
    }
}
