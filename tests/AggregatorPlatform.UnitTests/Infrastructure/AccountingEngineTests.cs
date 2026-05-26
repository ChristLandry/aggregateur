using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AggregatorPlatform.Infrastructure.Persistence;
using AggregatorPlatform.Infrastructure.Persistence.Repositories;
using AggregatorPlatform.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AggregatorPlatform.UnitTests.Infrastructure;

public class AccountingEngineTests
{
    private static (AggregatorDbContext db, AccountingEngine engine) BuildEngine()
    {
        var opts = new DbContextOptionsBuilder<AggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AggregatorDbContext(opts);
        var schemas = new AccountingSchemaRepository(db);
        var movements = new Repository<Movement>(db);
        var accounts = new PartnerAccountRepository(db);
        var accountMovements = new Repository<PartnerAccountMovement>(db);
        var partners = new PartnerRepository(db);
        var evaluator = new FormulaEvaluator();
        var engine = new AccountingEngine(schemas, movements, accounts, accountMovements, partners, evaluator,
            NullLogger<AccountingEngine>.Instance);
        return (db, engine);
    }

    [Fact]
    public async Task Applies_balanced_schema_and_creates_movements_with_signed_amounts()
    {
        var (db, engine) = BuildEngine();
        var partnerId = Guid.NewGuid();
        db.Partners.Add(new Partner { PartnerId = partnerId, PartnerCode = "P1", Name = "P", BaseUrl = "https://p", ApiKey = "h", Status = PartnerStatus.Active });
        db.PartnerAccounts.Add(new PartnerAccount { PartnerId = partnerId, Balance = 0, Currency = "XOF" });
        db.AccountingSchemas.Add(new AccountingSchema
        {
            Name = "BankDebit",
            TransactionType = TransactionType.BankDebit,
            TransactionSide = TransactionSide.Debit,
            Channel = Channel.Bank,
            IsActive = true,
            Lines = new List<AccountingSchemaLine>
            {
                new() { LineOrder = 1, AccountCode = "411", Side = LedgerSide.Debit,  AmountFormula = "AMOUNT",          Label = "Client",     Code = "CLI", Exploitant = "AU" },
                new() { LineOrder = 2, AccountCode = "70-FEE", Side = LedgerSide.Credit, AmountFormula = "AMOUNT * 0.05", Label = "Commission", Code = "FEE", Exploitant = "AU", IsFee = true },
                new() { LineOrder = 3, AccountCode = "707",   Side = LedgerSide.Credit, AmountFormula = "L1 - L2",        Label = "Vente nette",Code = "INTEG", Exploitant = "AU" },
            }
        });
        await db.SaveChangesAsync();

        var tx = new Transaction
        {
            PartnerId = partnerId,
            PartnerTransactionRef = "T1",
            TransactionType = TransactionType.BankDebit,
            Amount = 1000, FeeAmount = 0, NetAmount = 1000,
            Currency = "XOF",
            Status = TransactionStatus.Success
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await engine.ApplyAsync(tx);
        await db.SaveChangesAsync();

        tx.AccountingStatus.Should().Be(AccountingStatus.Applied);
        // Fee recalcule par le schema : 1000 * 5% = 50
        tx.FeeAmount.Should().Be(50);
        tx.NetAmount.Should().Be(950);

        var movements = db.Movements.OrderBy(m => m.LineOrder).ToList();
        movements.Should().HaveCount(3);
        movements[0].Amount.Should().Be(-1000); // debit
        movements[1].Amount.Should().Be(50);    // credit (fee)
        movements[2].Amount.Should().Be(950);   // credit (L1 - L2 = 1000 - 50)

        // Equilibre : -1000 + 50 + 950 = 0
        movements.Sum(m => m.Amount).Should().Be(0);
    }

    [Fact]
    public async Task Skips_when_no_schema_applicable()
    {
        var (db, engine) = BuildEngine();
        var partnerId = Guid.NewGuid();
        var tx = new Transaction
        {
            PartnerId = partnerId,
            PartnerTransactionRef = "X",
            TransactionType = TransactionType.BankCredit,
            Amount = 100, NetAmount = 100, Currency = "XOF"
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await engine.ApplyAsync(tx);
        tx.AccountingStatus.Should().Be(AccountingStatus.Pending);
    }

    [Fact]
    public async Task Updates_partner_mirror_account_on_bank_debit()
    {
        var (db, engine) = BuildEngine();
        var partnerId = Guid.NewGuid();
        db.PartnerAccounts.Add(new PartnerAccount { PartnerId = partnerId, Balance = 0, Currency = "XOF" });
        db.AccountingSchemas.Add(new AccountingSchema
        {
            Name = "S",
            TransactionType = TransactionType.BankDebit,
            TransactionSide = TransactionSide.Debit,
            Channel = Channel.Bank,
            IsActive = true,
            Lines = new List<AccountingSchemaLine>
            {
                new() { LineOrder = 1, AccountCode = "411", Side = LedgerSide.Debit,  AmountFormula = "AMOUNT", Label = "Client" },
                new() { LineOrder = 2, AccountCode = "707", Side = LedgerSide.Credit, AmountFormula = "AMOUNT", Label = "Vente" },
            }
        });
        var tx = new Transaction
        {
            PartnerId = partnerId,
            PartnerTransactionRef = "M1",
            TransactionType = TransactionType.BankDebit,
            Amount = 500, NetAmount = 500, Currency = "XOF",
            Status = TransactionStatus.Success
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await engine.ApplyAsync(tx);
        await db.SaveChangesAsync();

        var account = db.PartnerAccounts.Single(a => a.PartnerId == partnerId);
        account.Balance.Should().Be(500);
        db.PartnerAccountMovements.Should().HaveCount(1);
    }
}
