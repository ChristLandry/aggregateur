using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Infrastructure.Persistence;
using AggregatorPlatform.Infrastructure.Persistence.Repositories;
using AggregatorPlatform.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AggregatorPlatform.UnitTests.Infrastructure;

/// <summary>
/// Prouve que le lookup par (PartnerId, PhoneNumber, BankAccount) fonctionne
/// meme quand les colonnes sont chiffrees au repos via EncryptionValueConverter.
/// Le chiffrement AES-256 avec IV fixe est deterministe : EF Core applique le
/// ValueConverter au parametre du WHERE, la comparaison ciphertext-vs-ciphertext
/// reste egale ssi les plaintexts le sont.
/// </summary>
public class SubscriptionRepositoryEncryptionTests : IDisposable
{
    private readonly IEncryptionService? _previousEncryption;

    public SubscriptionRepositoryEncryptionTests()
    {
        _previousEncryption = EncryptionValueConverter.Encryption;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Cle AES-256 (32 octets) et IV (16 octets) fixes pour test.
                ["Encryption:Key"] = Convert.ToBase64String(new byte[32] {
                    1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,
                    17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32 }),
                ["Encryption:IV"]  = Convert.ToBase64String(new byte[16] {
                    1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 }),
            })
            .Build();
        EncryptionValueConverter.Encryption = new EncryptionService(config);
    }

    public void Dispose()
    {
        EncryptionValueConverter.Encryption = _previousEncryption;
    }

    private static AggregatorDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<AggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Finds_subscription_by_partner_phone_and_bank_despite_encryption()
    {
        using var db = BuildDb();
        var partnerId = Guid.NewGuid();
        var sub = new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            PartnerId = partnerId,
            CustomerId = Guid.NewGuid(),
            BankAccount = "ACC-42-CLEAR",
            PhoneNumber = "+221771234567",
            PhoneOperator = "WAVE",
            Status = SubscriptionStatus.Active,
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);
        var found = await repo.GetActiveSubscriptionByPartnerAndContactAsync(
            partnerId, "+221771234567", "ACC-42-CLEAR", CancellationToken.None);

        found.Should().NotBeNull();
        found!.SubscriptionId.Should().Be(sub.SubscriptionId);
        // Les proprietes lues sont dechiffrees automatiquement par le converter.
        found.BankAccount.Should().Be("ACC-42-CLEAR");
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
