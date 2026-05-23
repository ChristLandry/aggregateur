using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AggregatorPlatform.Infrastructure.Persistence;
using AggregatorPlatform.Infrastructure.Persistence.Repositories;
using AggregatorPlatform.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AggregatorPlatform.UnitTests.Infrastructure;

public class FeeCalculatorTests
{
    private static AggregatorDbContext BuildContext()
    {
        var opts = new DbContextOptionsBuilder<AggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AggregatorDbContext(opts);
    }

    [Fact]
    public async Task Returns_zero_when_no_configuration_found()
    {
        await using var db = BuildContext();
        var repo = new Repository<FeeConfiguration>(db);
        var calc = new FeeCalculator(repo);
        var fee = await calc.CalculateAsync(Guid.NewGuid(), TransactionType.BankDebit, 1000);
        fee.Should().Be(0);
    }

    [Fact]
    public async Task Applies_fixed_fee()
    {
        await using var db = BuildContext();
        db.FeeConfigurations.Add(new FeeConfiguration
        {
            PartnerId = null,
            TransactionType = TransactionType.BankDebit,
            FeeType = FeeType.Fixed,
            FixedAmount = 250,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repo = new Repository<FeeConfiguration>(db);
        var calc = new FeeCalculator(repo);
        var fee = await calc.CalculateAsync(Guid.NewGuid(), TransactionType.BankDebit, 10000);
        fee.Should().Be(250m);
    }

    [Fact]
    public async Task Applies_percentage_with_cap()
    {
        await using var db = BuildContext();
        db.FeeConfigurations.Add(new FeeConfiguration
        {
            PartnerId = null,
            TransactionType = TransactionType.BankDebit,
            FeeType = FeeType.Percentage,
            Percentage = 0.02m,
            MaxFeeAmount = 100,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repo = new Repository<FeeConfiguration>(db);
        var calc = new FeeCalculator(repo);
        var fee = await calc.CalculateAsync(Guid.NewGuid(), TransactionType.BankDebit, 100000);
        fee.Should().Be(100m); // capped from 2000 to 100
    }

    [Fact]
    public async Task Partner_specific_overrides_global()
    {
        await using var db = BuildContext();
        var partnerId = Guid.NewGuid();
        db.FeeConfigurations.AddRange(
            new FeeConfiguration
            {
                PartnerId = null, TransactionType = TransactionType.BankDebit, FeeType = FeeType.Fixed, FixedAmount = 100, IsActive = true
            },
            new FeeConfiguration
            {
                PartnerId = partnerId, TransactionType = TransactionType.BankDebit, FeeType = FeeType.Fixed, FixedAmount = 50, IsActive = true
            });
        await db.SaveChangesAsync();

        var calc = new FeeCalculator(new Repository<FeeConfiguration>(db));
        var fee = await calc.CalculateAsync(partnerId, TransactionType.BankDebit, 10000);
        fee.Should().Be(50m);
    }
}
