using AggregatorPlatform.Application.Interfaces;
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
        var entries = new Repository<JournalEntry>(db);
        var accounts = new PartnerAccountRepository(db);
        var movements = new Repository<PartnerAccountMovement>(db);
        var partners = new PartnerRepository(db);
        var evaluator = new FormulaEvaluator();
        var engine = new AccountingEngine(schemas, entries, accounts, movements, partners, evaluator,
            NullLogger<AccountingEngine>.Instance);
        return (db, engine);
    }

    [Fact]
    public async Task Applies_balanced_schema_and_creates_journal()
    {
        var (db, engine) = BuildEngine();
        var partnerId = Guid.NewGuid();
        db.Partners.Add(new Partner { PartnerId = partnerId, PartnerCode = "P1", Name = "P", BaseUrl = "https://p", ApiKey = "h", Status = PartnerStatus.Active });
        db.PartnerAccounts.Add(new PartnerAccount { PartnerId = partnerId, Balance = 0, Currency = "XOF" });
        var schemaId = Guid.NewGuid();
        db.AccountingSchemas.Add(new AccountingSchema
        {
            SchemaId = schemaId,
            Name = "BankDebit",
            TransactionType = TransactionType.BankDebit,
            TransactionSide = TransactionSide.Debit,
            Channel = Channel.Bank,
            IsActive = true,
            Lines = new List<AccountingSchemaLine>
            {
                new() { LineOrder = 1, AccountCode = "411", Side = LedgerSide.Debit, AmountFormula = "AMOUNT", Label = "Client" },
                new() { LineOrder = 2, AccountCode = "707", Side = LedgerSide.Credit, AmountFormula = "AMOUNT_NET", Label = "Vente" },
                new() { LineOrder = 3, AccountCode = "70", Side = LedgerSide.Credit, AmountFormula = "FEE", Label = "Commission" }
            }
        });
        await db.SaveChangesAsync();

        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            PartnerId = partnerId,
            PartnerTransactionRef = "T1",
            TransactionType = TransactionType.BankDebit,
            Amount = 1000, FeeAmount = 50, NetAmount = 950,
            Currency = "XOF",
            Status = TransactionStatus.Success
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await engine.ApplyAsync(tx);
        await db.SaveChangesAsync();

        tx.AccountingStatus.Should().Be(AccountingStatus.Applied);
        var entry = db.JournalEntries.Include(e => e.Lines).Single();
        entry.IsBalanced.Should().BeTrue();
        entry.TotalDebit.Should().Be(1000);
        entry.TotalCredit.Should().Be(1000);
        entry.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task Marks_error_when_no_schema_applicable()
    {
        var (db, engine) = BuildEngine();
        var partnerId = Guid.NewGuid();
        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            PartnerId = partnerId,
            TransactionType = TransactionType.BankCredit,
            Amount = 100, NetAmount = 100, Currency = "XOF"
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await engine.ApplyAsync(tx);
        tx.AccountingStatus.Should().Be(AccountingStatus.Pending);
    }

    [Fact]
    public async Task Updates_mirror_account_on_bank_debit()
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
                new() { LineOrder = 1, AccountCode = "411", Side = LedgerSide.Debit, AmountFormula = "AMOUNT", Label = "Client" },
                new() { LineOrder = 2, AccountCode = "707", Side = LedgerSide.Credit, AmountFormula = "AMOUNT", Label = "Vente" }
            }
        });
        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            PartnerId = partnerId,
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
