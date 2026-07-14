using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Accounting.Commands;

public record CreateAccountingSchemaCommand(CreateAccountingSchemaRequest Request) : IRequest<Result<Guid>>;

public class CreateAccountingSchemaValidator : AbstractValidator<CreateAccountingSchemaCommand>
{
    public CreateAccountingSchemaValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Priority).GreaterThanOrEqualTo(0);
        // Lines optionnelles à la création : les lignes s'ajoutent ensuite via POST .../lines
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountCode).NotEmpty().MaximumLength(50);
            line.RuleFor(l => l.AmountFormula).NotEmpty().MaximumLength(500);
            line.RuleFor(l => l.Label).NotEmpty().MaximumLength(200);
        }).When(x => x.Request.Lines is { Count: > 0 });
    }
}

public class CreateAccountingSchemaCommandHandler : IRequestHandler<CreateAccountingSchemaCommand, Result<Guid>>
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IUnitOfWork _uow;

    public CreateAccountingSchemaCommandHandler(IAccountingSchemaRepository schemas, IUnitOfWork uow)
    {
        _schemas = schemas;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreateAccountingSchemaCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Request.Lines ?? Array.Empty<CreateAccountingSchemaLineRequest>();
        var schema = new AccountingSchema
        {
            Name = request.Request.Name,
            PartnerId = request.Request.PartnerId,
            TransactionType = request.Request.TransactionType,
            TransactionSide = request.Request.TransactionSide,
            Channel = request.Request.Channel,
            Priority = request.Request.Priority,
            Description = request.Request.Description,
            IsActive = true,
            Lines = lines.Select(l => new AccountingSchemaLine
            {
                LineOrder = l.LineOrder,
                AccountCode = l.AccountCode,
                AccountType = l.AccountType,
                AccountExpression = l.AccountExpression,
                Side = l.Side,
                AmountFormula = l.AmountFormula,
                Label = l.Label,
                Code = l.Code,
                Exploitant = l.Exploitant,
                IsFee = l.IsFee,
                IsConditional = l.IsConditional,
                Condition = l.Condition
            }).ToList()
        };
        await _schemas.AddAsync(schema, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(schema.SchemaId);
    }
}

public record UpdateAccountingSchemaCommand(Guid SchemaId, UpdateAccountingSchemaRequest Request) : IRequest<Result>;

/// <summary>
/// Validation conditionnelle : chaque regle ne s'applique que si la propriete est presente.
/// </summary>
public class UpdateAccountingSchemaValidator : AbstractValidator<UpdateAccountingSchemaCommand>
{
    public UpdateAccountingSchemaValidator()
    {
        When(x => x.Request.Name is not null, () =>
        {
            RuleFor(x => x.Request.Name!).NotEmpty().MaximumLength(200);
        });

        When(x => x.Request.Priority.HasValue, () =>
        {
            RuleFor(x => x.Request.Priority!.Value).GreaterThanOrEqualTo(0);
        });
    }
}

public class UpdateAccountingSchemaCommandHandler : IRequestHandler<UpdateAccountingSchemaCommand, Result>
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IUnitOfWork _uow;

    public UpdateAccountingSchemaCommandHandler(IAccountingSchemaRepository schemas, IUnitOfWork uow)
    {
        _schemas = schemas;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateAccountingSchemaCommand request, CancellationToken cancellationToken)
    {
        var schema = await _schemas.GetByIdAsync(request.SchemaId, cancellationToken);
        if (schema is null) return Result.Failure("SCHEMA_NOT_FOUND", "Schema not found.");

        var r = request.Request;

        // PATCH partiel : seuls les champs explicitement fournis sont appliques.
        if (r.Name        is not null) schema.Name        = r.Name;
        if (r.IsActive.HasValue)       schema.IsActive    = r.IsActive.Value;
        if (r.Priority.HasValue)       schema.Priority    = r.Priority.Value;
        if (r.Description is not null) schema.Description = r.Description;

        _schemas.Update(schema);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record AddSchemaLineCommand(Guid SchemaId, CreateAccountingSchemaLineRequest Line) : IRequest<Result<Guid>>;

public class AddSchemaLineValidator : AbstractValidator<AddSchemaLineCommand>
{
    public AddSchemaLineValidator()
    {
        RuleFor(x => x.Line.LineOrder).GreaterThan(0);
        RuleFor(x => x.Line.AmountFormula).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Line.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line.AccountCode)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Line.AccountType == Domain.Enums.AccountType.Fixed);
        RuleFor(x => x.Line.AccountExpression)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => x.Line.AccountType == Domain.Enums.AccountType.Dynamic);
    }
}

public class AddSchemaLineCommandHandler : IRequestHandler<AddSchemaLineCommand, Result<Guid>>
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IRepository<AccountingSchemaLine> _lines;
    private readonly IUnitOfWork _uow;

    public AddSchemaLineCommandHandler(IAccountingSchemaRepository schemas,
        IRepository<AccountingSchemaLine> lines, IUnitOfWork uow)
    {
        _schemas = schemas;
        _lines = lines;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(AddSchemaLineCommand request, CancellationToken cancellationToken)
    {
        var schema = await _schemas.GetByIdAsync(request.SchemaId, cancellationToken);
        if (schema is null) return Result<Guid>.Failure("SCHEMA_NOT_FOUND", "Schema not found.");

        // Unicite metier : LineOrder unique par schema (parmi les lignes non supprimees).
        var duplicate = await _lines.ExistsAsync(
            l => l.SchemaId == request.SchemaId && l.LineOrder == request.Line.LineOrder,
            cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure("LINE_ORDER_DUPLICATE",
                $"LineOrder {request.Line.LineOrder} is already used in this schema.");

        var line = new AccountingSchemaLine
        {
            SchemaId = request.SchemaId,
            LineOrder = request.Line.LineOrder,
            AccountCode = request.Line.AccountCode,
            AccountType = request.Line.AccountType,
            AccountExpression = request.Line.AccountExpression,
            Side = request.Line.Side,
            AmountFormula = request.Line.AmountFormula,
            Label = request.Line.Label,
            Code = request.Line.Code,
            Exploitant = request.Line.Exploitant,
            IsFee = request.Line.IsFee,
            IsConditional = request.Line.IsConditional,
            Condition = request.Line.Condition
        };
        await _lines.AddAsync(line, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(line.LineId);
    }
}

public record RemoveSchemaLineCommand(Guid SchemaId, Guid LineId) : IRequest<Result>;

public class RemoveSchemaLineCommandHandler : IRequestHandler<RemoveSchemaLineCommand, Result>
{
    private readonly IRepository<AccountingSchemaLine> _lines;
    private readonly IUnitOfWork _uow;

    public RemoveSchemaLineCommandHandler(IRepository<AccountingSchemaLine> lines, IUnitOfWork uow)
    {
        _lines = lines;
        _uow = uow;
    }

    public async Task<Result> Handle(RemoveSchemaLineCommand request, CancellationToken cancellationToken)
    {
        var line = await _lines.GetByIdAsync(request.LineId, cancellationToken);
        if (line is null || line.SchemaId != request.SchemaId)
            return Result.Failure("LINE_NOT_FOUND", "Schema line not found.");
        _lines.Remove(line);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record UpdateSchemaLineCommand(Guid SchemaId, Guid LineId, UpdateAccountingSchemaLineRequest Line)
    : IRequest<Result>;

public class UpdateSchemaLineValidator : AbstractValidator<UpdateSchemaLineCommand>
{
    public UpdateSchemaLineValidator()
    {
        RuleFor(x => x.Line.AmountFormula).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Line.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line.LineOrder).GreaterThan(0);
        RuleFor(x => x.Line.AccountCode)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Line.AccountType == Domain.Enums.AccountType.Fixed);
        RuleFor(x => x.Line.AccountExpression)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => x.Line.AccountType == Domain.Enums.AccountType.Dynamic);
    }
}

public class UpdateSchemaLineCommandHandler : IRequestHandler<UpdateSchemaLineCommand, Result>
{
    private readonly IRepository<AccountingSchemaLine> _lines;
    private readonly IUnitOfWork _uow;

    public UpdateSchemaLineCommandHandler(IRepository<AccountingSchemaLine> lines, IUnitOfWork uow)
    {
        _lines = lines;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateSchemaLineCommand request, CancellationToken cancellationToken)
    {
        var line = await _lines.GetByIdAsync(request.LineId, cancellationToken);
        if (line is null || line.SchemaId != request.SchemaId)
            return Result.Failure("LINE_NOT_FOUND", "Schema line not found.");

        // Unicite LineOrder par schema (hors ligne courante).
        if (line.LineOrder != request.Line.LineOrder)
        {
            var duplicate = await _lines.ExistsAsync(
                l => l.SchemaId == request.SchemaId
                     && l.LineId != request.LineId
                     && l.LineOrder == request.Line.LineOrder,
                cancellationToken);
            if (duplicate)
                return Result.Failure("LINE_ORDER_DUPLICATE",
                    $"LineOrder {request.Line.LineOrder} is already used in this schema.");
        }

        line.LineOrder = request.Line.LineOrder;
        line.AccountCode = request.Line.AccountCode;
        line.AccountType = request.Line.AccountType;
        line.AccountExpression = request.Line.AccountExpression;
        line.Side = request.Line.Side;
        line.AmountFormula = request.Line.AmountFormula;
        line.Label = request.Line.Label;
        line.Code = request.Line.Code;
        line.Exploitant = request.Line.Exploitant;
        line.IsFee = request.Line.IsFee;
        line.IsConditional = request.Line.IsConditional;
        line.Condition = request.Line.Condition;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
