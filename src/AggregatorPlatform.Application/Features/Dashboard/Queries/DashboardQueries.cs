using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery() : IRequest<Result<AdminDashboardSummaryDto>>;
public record GetPartnerDashboardQuery(Guid PartnerId) : IRequest<Result<PartnerDashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<AdminDashboardSummaryDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ICustomerRepository _customers;
    private readonly ISubscriptionRepository _subs;
    private readonly ITransactionRepository _txs;
    private readonly ICacheService _cache;

    public GetDashboardSummaryQueryHandler(IPartnerRepository partners, ICustomerRepository customers,
        ISubscriptionRepository subs, ITransactionRepository txs, ICacheService cache)
    {
        _partners = partners;
        _customers = customers;
        _subs = subs;
        _txs = txs;
        _cache = cache;
    }

    public async Task<Result<AdminDashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "dashboard:admin:summary";
        var cached = await _cache.GetAsync<AdminDashboardSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<AdminDashboardSummaryDto>.Success(cached);

        var today = DateTime.UtcNow.Date;
        var last24h = DateTime.UtcNow.AddHours(-24);

        var totalPartners = await _partners.CountAsync(cancellationToken: cancellationToken);
        var activePartners = await _partners.CountAsync(p => p.Status == PartnerStatus.Active, cancellationToken);
        var totalCustomers = await _customers.CountAsync(cancellationToken: cancellationToken);
        var totalSubs = await _subs.CountAsync(cancellationToken: cancellationToken);

        var todayTxs = _txs.Query().Where(t => t.InitiatedAt >= today);
        var todayCount = await todayTxs.CountAsync(cancellationToken);
        var todayVolume = await todayTxs.Where(t => t.Status == TransactionStatus.Success).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;

        var pending = await _txs.CountAsync(t => t.Status == TransactionStatus.Pending, cancellationToken);
        var failed24 = await _txs.CountAsync(t => t.Status == TransactionStatus.Failed && t.InitiatedAt >= last24h, cancellationToken);

        var dto = new AdminDashboardSummaryDto(totalPartners, activePartners, totalCustomers, totalSubs,
            todayCount, todayVolume, pending, failed24);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromSeconds(30), cancellationToken);
        return Result<AdminDashboardSummaryDto>.Success(dto);
    }
}

public class GetPartnerDashboardQueryHandler : IRequestHandler<GetPartnerDashboardQuery, Result<PartnerDashboardSummaryDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly IPartnerAccountRepository _accounts;
    private readonly ITransactionRepository _txs;
    private readonly ISubscriptionRepository _subs;

    public GetPartnerDashboardQueryHandler(IPartnerRepository partners, IPartnerAccountRepository accounts,
        ITransactionRepository txs, ISubscriptionRepository subs)
    {
        _partners = partners;
        _accounts = accounts;
        _txs = txs;
        _subs = subs;
    }

    public async Task<Result<PartnerDashboardSummaryDto>> Handle(GetPartnerDashboardQuery request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<PartnerDashboardSummaryDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var account = await _accounts.GetByPartnerIdAsync(request.PartnerId, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var todayTxs = _txs.Query().Where(t => t.PartnerId == request.PartnerId && t.InitiatedAt >= today);
        var todayCount = await todayTxs.CountAsync(cancellationToken);
        var todayVolume = await todayTxs.Where(t => t.Status == TransactionStatus.Success).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;
        var activeSubs = await _subs.CountAsync(s => s.PartnerId == request.PartnerId && s.Status == SubscriptionStatus.Active, cancellationToken);

        return Result<PartnerDashboardSummaryDto>.Success(new PartnerDashboardSummaryDto(
            partner.PartnerId, partner.PartnerCode, account?.Balance ?? 0, todayCount, todayVolume, activeSubs));
    }
}
