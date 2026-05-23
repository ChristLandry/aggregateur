using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Infrastructure.Services;

public class FeeCalculator : IFeeCalculator
{
    private readonly IRepository<FeeConfiguration> _fees;

    public FeeCalculator(IRepository<FeeConfiguration> fees) => _fees = fees;

    public async Task<decimal> CalculateAsync(Guid partnerId, TransactionType type, decimal amount, CancellationToken cancellationToken = default)
    {
        var partnerFee = await _fees.Query().AsNoTracking()
            .Where(f => f.PartnerId == partnerId && f.TransactionType == type && f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var fee = partnerFee ?? await _fees.Query().AsNoTracking()
            .Where(f => f.PartnerId == null && f.TransactionType == type && f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (fee is null) return 0m;

        var result = fee.FeeType switch
        {
            FeeType.Fixed => fee.FixedAmount,
            FeeType.Percentage => Math.Round(amount * fee.Percentage, 0),
            FeeType.Mixed => fee.FixedAmount + Math.Round(amount * fee.Percentage, 0),
            _ => 0m
        };

        if (fee.MaxFeeAmount.HasValue && result > fee.MaxFeeAmount.Value)
            result = fee.MaxFeeAmount.Value;

        return result;
    }
}
