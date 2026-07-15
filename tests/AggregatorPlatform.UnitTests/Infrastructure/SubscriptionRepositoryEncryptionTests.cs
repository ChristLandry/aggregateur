using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Infrastructure.Persistence;
using AggregatorPlatform.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AggregatorPlatform.UnitTests.Infrastructure;

/// <summary>
/// Verifie le lookup par (PartnerId, PhoneNumber, BankAccount) sur SubscriptionRepository.
/// BankAccount et PhoneNumber sont desormais stockes en clair : la comparaison EF Core
/// est directe, sans ValueConverter, et le test doit rester vert pour couvrir la regle
/// metier des endpoints /api/v1/bank/*.
/// </summary>
public class SubscriptionRepositoryLookupTests
{
    private static AggregatorDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<AggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Finds_active_subscription_by_partner_phone_and_bank()
    {
        using var db = BuildDb();
        var partnerId = Guid.NewGuid();
        var sub = new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            PartnerId = partnerId,
            CustomerId = Guid.NewGuid(),
            BankAccount = "ACC-42",
            PhoneNumber = "+221771234567",
            PhoneOperator = "WAVE",
            Status = SubscriptionStatus.Active,
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);
        var found = await repo.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, "+221771234567", "ACC-42", CancellationToken.None);

        found.Should().NotBeNull();
        found!.SubscriptionId.Should().Be(sub.SubscriptionId);
        found.BankAccount.Should().Be("ACC-42");
        found.PhoneNumber.Should().Be("+221771234567");
    }

    [Fact]
    public async Task Returns_null_when_phone_or_bank_do_not_match()
    {
        using var db = BuildDb();
        var partnerId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            PartnerId = partnerId,
            CustomerId = Guid.NewGuid(),
            BankAccount = "ACC-42",
            PhoneNumber = "+221771111111",
            PhoneOperator = "WAVE",
            Status = SubscriptionStatus.Active,
        });
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);
        var wrongPhone = await repo.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, "+221779999999", "ACC-42", CancellationToken.None);
        var wrongBank = await repo.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, "+221771111111", "ACC-99", CancellationToken.None);

        wrongPhone.Should().BeNull();
        wrongBank.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_subscription_is_inactive()
    {
        using var db = BuildDb();
        var partnerId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            PartnerId = partnerId,
            CustomerId = Guid.NewGuid(),
            BankAccount = "ACC-42",
            PhoneNumber = "+22177",
            PhoneOperator = "WAVE",
            Status = SubscriptionStatus.Suspended,
        });
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);
        var found = await repo.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, "+22177", "ACC-42", CancellationToken.None);

        found.Should().BeNull();
    }
}
