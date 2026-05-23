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
    bool IsConditional,
    string? Condition);

public record UpdateAccountingSchemaRequest(
    string Name,
    bool IsActive,
    int Priority,
    string? Description);

public record JournalEntryDto(
    Guid EntryId,
    Guid TransactionId,
    Guid SchemaId,
    DateTime EntryDate,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced,
    IReadOnlyList<JournalLineDto> Lines);

public record JournalLineDto(
    Guid LineId,
    string AccountCode,
    LedgerSide Side,
    decimal Amount,
    string Label);
