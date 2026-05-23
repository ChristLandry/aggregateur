using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.Services;

public class AccountingEngine : IAccountingEngine
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IRepository<JournalEntry> _entries;
    private readonly IPartnerAccountRepository _accounts;
    private readonly IRepository<PartnerAccountMovement> _movements;
    private readonly IPartnerRepository _partners;
    private readonly IFormulaEvaluator _evaluator;
    private readonly ILogger<AccountingEngine> _logger;

    public AccountingEngine(
        IAccountingSchemaRepository schemas,
        IRepository<JournalEntry> entries,
        IPartnerAccountRepository accounts,
        IRepository<PartnerAccountMovement> movements,
        IPartnerRepository partners,
        IFormulaEvaluator evaluator,
        ILogger<AccountingEngine> logger)
    {
        _schemas = schemas;
        _entries = entries;
        _accounts = accounts;
        _movements = movements;
        _partners = partners;
        _evaluator = evaluator;
        _logger = logger;
    }

    public async Task ApplyAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var (side, channel) = ResolveSideChannel(transaction.TransactionType);

        var schema = await _schemas.SelectApplicableSchemaAsync(transaction.PartnerId,
            transaction.TransactionType, side, channel, cancellationToken);

        if (schema is null)
        {
            _logger.LogWarning("No accounting schema found for tx {TxId} type={Type} side={Side} channel={Channel}",
                transaction.TransactionId, transaction.TransactionType, side, channel);
            transaction.AccountingStatus = AccountingStatus.Pending;
            return;
        }

        var partner = await _partners.GetByIdAsync(transaction.PartnerId, cancellationToken);
        var partnerAccount = await _accounts.GetByPartnerIdAsync(transaction.PartnerId, cancellationToken);
        var context = BuildContext(transaction, partner, partnerAccount, transaction.Subscription);

        var entry = new JournalEntry
        {
            TransactionId = transaction.TransactionId,
            SchemaId = schema.SchemaId,
            EntryDate = DateTime.UtcNow
        };

        try
        {
            foreach (var line in schema.Lines.OrderBy(l => l.LineOrder))
            {
                if (line.IsConditional && !string.IsNullOrEmpty(line.Condition))
                {
                    if (!_evaluator.EvaluateCondition(line.Condition, context))
                        continue;
                }

                var accountCode = line.AccountType == AccountType.Dynamic && !string.IsNullOrEmpty(line.AccountExpression)
                    ? _evaluator.EvaluateExpression(line.AccountExpression, context)
                    : line.AccountCode;

                var amount = _evaluator.EvaluateAmount(line.AmountFormula, context);

                entry.Lines.Add(new JournalLine
                {
                    AccountCode = accountCode,
                    Side = line.Side,
                    Amount = amount,
                    Label = line.Label
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accounting schema evaluation failed for tx {TxId}", transaction.TransactionId);
            transaction.AccountingStatus = AccountingStatus.Error;
            return;
        }

        entry.TotalDebit = entry.Lines.Where(l => l.Side == LedgerSide.Debit).Sum(l => l.Amount);
        entry.TotalCredit = entry.Lines.Where(l => l.Side == LedgerSide.Credit).Sum(l => l.Amount);
        entry.IsBalanced = entry.TotalDebit == entry.TotalCredit;

        if (!entry.IsBalanced)
        {
            _logger.LogError("Journal entry is unbalanced for tx {TxId}: D={Debit} C={Credit}",
                transaction.TransactionId, entry.TotalDebit, entry.TotalCredit);
            transaction.AccountingStatus = AccountingStatus.Error;
            return;
        }

        await _entries.AddAsync(entry, cancellationToken);

        transaction.SchemaId = schema.SchemaId;
        transaction.AccountingStatus = AccountingStatus.Applied;

        // Update mirror account
        if (partnerAccount is not null)
        {
            await UpdateMirrorAccountAsync(transaction, partnerAccount, cancellationToken);
        }
    }

    private async Task UpdateMirrorAccountAsync(Transaction tx, PartnerAccount account, CancellationToken ct)
    {
        var (movType, amount) = tx.TransactionType switch
        {
            TransactionType.BankDebit => (MovementType.Credit, tx.NetAmount),
            TransactionType.WalletDebit => (MovementType.Credit, tx.NetAmount),
            TransactionType.BankCredit => (MovementType.Debit, tx.NetAmount),
            TransactionType.WalletCredit => (MovementType.Debit, tx.NetAmount),
            TransactionType.WalletCancel => (MovementType.Debit, tx.NetAmount),
            _ => (MovementType.Credit, 0m)
        };

        if (amount == 0) return;

        var before = account.Balance;
        account.Balance = movType == MovementType.Credit ? before + amount : before - amount;
        account.LastMovementAt = DateTime.UtcNow;
        _accounts.Update(account);

        await _movements.AddAsync(new PartnerAccountMovement
        {
            PartnerId = tx.PartnerId,
            TransactionId = tx.TransactionId,
            MovementType = movType,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = account.Balance,
            MovementDate = DateTime.UtcNow,
            Description = $"{tx.TransactionType} - {tx.PartnerTransactionRef}"
        }, ct);
    }

    private static (TransactionSide, Channel) ResolveSideChannel(TransactionType type) => type switch
    {
        TransactionType.BankDebit => (TransactionSide.Debit, Channel.Bank),
        TransactionType.BankCredit => (TransactionSide.Credit, Channel.Bank),
        TransactionType.WalletDebit => (TransactionSide.Debit, Channel.Wallet),
        TransactionType.WalletCredit => (TransactionSide.Credit, Channel.Wallet),
        TransactionType.WalletCancel => (TransactionSide.Credit, Channel.Wallet),
        _ => (TransactionSide.Debit, Channel.Bank)
    };

    private static IDictionary<string, object?> BuildContext(Transaction tx, Partner? partner, PartnerAccount? account, Subscription? sub)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AMOUNT"] = tx.Amount,
            ["AMOUNT_NET"] = tx.NetAmount,
            ["FEE"] = tx.FeeAmount,
            ["PARTNER.Balance"] = account?.Balance ?? 0m,
            ["PARTNER.AccountCode"] = partner?.AccountCode ?? "DEFAULT",
            ["CUSTOMER.PhoneNumber"] = sub?.PhoneNumber ?? "",
            ["TX.Currency"] = tx.Currency,
            ["TX.Type"] = tx.TransactionType.ToString()
        };
    }
}
