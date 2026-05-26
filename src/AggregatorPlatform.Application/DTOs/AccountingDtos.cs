using AggregatorPlatform.Domain.Enums;

namespace AggregatorPlatform.Application.DTOs;

public record AccountingSchemaDto(
    Guid SchemaId,
    string Name,
    Guid? PartnerId,
    TransactionType TransactionType,
    TransactionSide TransactionSide,
    Channel Channel,
    bool IsActive,
    int Priority,
    string? Description,
    IReadOnlyList<AccountingSchemaLineDto> Lines);

public record AccountingSchemaLineDto(
    Guid LineId,
    int LineOrder,
    string AccountCode,
    AccountType AccountType,
    string? AccountExpression,
    LedgerSide Side,
    string AmountFormula,
    string Label,
    string? Code,
    string? Exploitant,
    bool IsFee,
    bool IsConditional,
    string? Condition);

public record CreateAccountingSchemaRequest(
    string Name,
    Guid? PartnerId,
    TransactionType TransactionType,
    TransactionSide TransactionSide,
    Channel Channel,
    int Priority,
    string? Description,
    IReadOnlyList<CreateAccountingSchemaLineRequest> Lines);

public record CreateAccountingSchemaLineRequest(
    int LineOrder,
    string AccountCode,
    AccountType AccountType,
    string? AccountExpression,
    LedgerSide Side,
    string AmountFormula,
    string Label,
    string? Code,
    string? Exploitant,
    bool IsFee,
    bool IsConditional,
    string? Condition);

/// <summary>
/// PATCH partiel : seuls les champs renseignes sont appliques.
/// </summary>
public record UpdateAccountingSchemaRequest(
    string? Name,
    bool? IsActive,
    int? Priority,
    string? Description);

/// <summary>
/// Mouvement comptable genere par l'application d'un schema sur une transaction.
/// </summary>
public record MovementDto(
    Guid MovementId,
    Guid TransactionId,
    Guid SchemaId,
    int LineOrder,
    string Account,
    decimal Amount,
    LedgerSide Side,
    string Label,
    string? Code,
    string? Exploitant,
    string? Reference,
    DateTime TransactionDate,
    bool IsFee);
