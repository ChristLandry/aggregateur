using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.Services;

/// <summary>
/// Applique un schema comptable a une transaction pour generer N mouvements.
///
/// Regles :
///   - Une transaction declenche le schema applicable au triplet (PartnerId, TransactionType, TransactionSide, Channel).
///   - Chaque ligne du schema est evaluee dans l'ordre LineOrder croissant.
///   - Variables disponibles dans les formules :
///       AMOUNT, AMOUNT_NET, FEE, PARTNER.Balance, CUSTOMER.PhoneNumber,
///       TX.Currency, TX.Type, et L1, L2, ... LN (montants deja calcules des lignes precedentes).
///   - Les lignes marquees IsFee sont sommees dans Transaction.FeeAmount,
///     Transaction.NetAmount est recalcule (= Amount - FeeAmount).
///   - Chaque ligne genere un Movement (account, signed amount, label, code, exploitant, reference, transactionDate).
///   - Le solde miroir du partenaire (PartnerAccount) est ajuste via PartnerAccountMovement.
/// </summary>
public class AccountingEngine : IAccountingEngine
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IRepository<Movement> _movements;
    private readonly IPartnerAccountRepository _accounts;
    private readonly IRepository<PartnerAccountMovement> _accountMovements;
    private readonly IPartnerRepository _partners;
    private readonly IFormulaEvaluator _evaluator;
    private readonly ILogger<AccountingEngine> _logger;

    public AccountingEngine(
        IAccountingSchemaRepository schemas,
        IRepository<Movement> movements,
        IPartnerAccountRepository accounts,
        IRepository<PartnerAccountMovement> accountMovements,
        IPartnerRepository partners,
        IFormulaEvaluator evaluator,
        ILogger<AccountingEngine> logger)
    {
        _schemas = schemas;
        _movements = movements;
        _accounts = accounts;
        _accountMovements = accountMovements;
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
            _logger.LogWarning("No accounting schema for tx {TxId} type={Type} side={Side} channel={Channel}",
                transaction.TransactionId, transaction.TransactionType, side, channel);
            transaction.AccountingStatus = AccountingStatus.Pending;
            return;
        }

        var partner = await _partners.GetByIdAsync(transaction.PartnerId, cancellationToken);
        var partnerAccount = await _accounts.GetByPartnerIdAsync(transaction.PartnerId, cancellationToken);
        var context = BuildContext(transaction, partner, partnerAccount, transaction.Subscription);

        var generated = new List<Movement>();
        decimal feeTotal = 0m;

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

                var rawAmount = _evaluator.EvaluateAmount(line.AmountFormula, context);

                // Convention : amount signe (negatif pour debit, positif pour credit).
                var signedAmount = line.Side == LedgerSide.Debit ? -Math.Abs(rawAmount) : Math.Abs(rawAmount);

                var movement = new Movement
                {
                    TransactionId = transaction.TransactionId,
                    SchemaId = schema.SchemaId,
                    LineOrder = line.LineOrder,
                    Account = accountCode,
                    Amount = signedAmount,
                    Side = line.Side,
                    Label = line.Label,
                    Code = line.Code,
                    Exploitant = line.Exploitant,
                    Reference = transaction.PartnerTransactionRef,
                    TransactionDate = DateTime.UtcNow,
                    IsFee = line.IsFee,
                };
                generated.Add(movement);

                // Expose le montant calcule (valeur absolue) aux lignes suivantes via L<N>.
                context[$"L{line.LineOrder}"] = Math.Abs(rawAmount);

                if (line.IsFee) feeTotal += Math.Abs(rawAmount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema evaluation failed for tx {TxId}", transaction.TransactionId);
            transaction.AccountingStatus = AccountingStatus.Error;
            return;
        }

        // Si aucune ligne IsFee : conserve la valeur deja presente (override de la requete par exemple).
        if (generated.Any(m => m.IsFee))
        {
            transaction.FeeAmount = feeTotal;
            transaction.NetAmount = transaction.Amount - feeTotal;
        }

        // Verification d'equilibre comptable : somme des montants signes doit etre 0.
        var balance = generated.Sum(m => m.Amount);
        if (balance != 0m)
        {
            _logger.LogError("Movements unbalanced for tx {TxId}: signed sum = {Sum} (expected 0)",
                transaction.TransactionId, balance);
            transaction.AccountingStatus = AccountingStatus.Error;
            return;
        }

        foreach (var m in generated)
            await _movements.AddAsync(m, cancellationToken);

        transaction.SchemaId = schema.SchemaId;
        transaction.AccountingStatus = AccountingStatus.Applied;

        // Met a jour le solde miroir du partenaire.
        if (partnerAccount is not null)
        {
            await UpdateMirrorAccountAsync(transaction, partnerAccount, cancellationToken);
        }
    }

    private async Task UpdateMirrorAccountAsync(Transaction tx, PartnerAccount account, CancellationToken ct)
    {
        var (movType, amount) = tx.TransactionType switch
        {
            TransactionType.BankDebit    => (MovementType.Credit, tx.NetAmount),
            TransactionType.WalletDebit  => (MovementType.Credit, tx.NetAmount),
            TransactionType.BankCredit   => (MovementType.Debit,  tx.NetAmount),
            TransactionType.WalletCredit => (MovementType.Debit,  tx.NetAmount),
            TransactionType.WalletCancel => (MovementType.Debit,  tx.NetAmount),
            _ => (MovementType.Credit, 0m)
        };

        if (amount == 0) return;

        var before = account.Balance;
        account.Balance = movType == MovementType.Credit ? before + amount : before - amount;
        account.LastMovementAt = DateTime.UtcNow;
        _accounts.Update(account);

        await _accountMovements.AddAsync(new PartnerAccountMovement
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
        TransactionType.BankDebit    => (TransactionSide.Debit,  Channel.Bank),
        TransactionType.BankCredit   => (TransactionSide.Credit, Channel.Bank),
        TransactionType.WalletDebit  => (TransactionSide.Debit,  Channel.Wallet),
        TransactionType.WalletCredit => (TransactionSide.Credit, Channel.Wallet),
        TransactionType.WalletCancel => (TransactionSide.Credit, Channel.Wallet),
        _ => (TransactionSide.Debit, Channel.Bank)
    };

    private static IDictionary<string, object?> BuildContext(Transaction tx, Partner? partner, PartnerAccount? account, Subscription? sub)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AMOUNT"]                = tx.Amount,
            ["AMOUNT_NET"]            = tx.NetAmount,
            ["FEE"]                   = tx.FeeAmount,
            ["PARTNER.Balance"]       = account?.Balance ?? 0m,
            ["PARTNER.AccountCode"]   = partner?.AccountCode ?? "DEFAULT",
            ["CUSTOMER.PhoneNumber"]  = sub?.PhoneNumber ?? string.Empty,
            ["CUSTOMER.BankAccount"]  = sub?.BankAccountNumber ?? string.Empty,
            ["TX.Currency"]           = tx.Currency,
            ["TX.Type"]               = tx.TransactionType.ToString(),
        };
    }
}
