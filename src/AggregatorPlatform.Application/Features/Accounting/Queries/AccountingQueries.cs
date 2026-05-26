using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Accounting.Queries;

public record GetAllSchemasQuery() : IRequest<Result<IReadOnlyList<AccountingSchemaDto>>>;
public record GetSchemaByIdQuery(Guid SchemaId) : IRequest<Result<AccountingSchemaDto>>;

/// <summary>Liste paginee des mouvements comptables (filtres date / compte / transaction).</summary>
public record GetMovementsQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    string? Account,
    Guid? TransactionId,
    int Page = 1,
    int PageSize = 50)
    : IRequest<Result<PaginatedResult<MovementDto>>>;

/// <summary>Tous les mouvements d'une transaction donnee.</summary>
public record GetMovementsByTransactionQuery(Guid TransactionId)
    : IRequest<Result<IReadOnlyList<MovementDto>>>;

public class GetAllSchemasQueryHandler : IRequestHandler<GetAllSchemasQuery, Result<IReadOnlyList<AccountingSchemaDto>>>
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IMapper _mapper;

    public GetAllSchemasQueryHandler(IAccountingSchemaRepository schemas, IMapper mapper)
    {
        _schemas = schemas;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<AccountingSchemaDto>>> Handle(GetAllSchemasQuery request, CancellationToken cancellationToken)
    {
        var items = await _schemas.Query().Include(s => s.Lines).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<AccountingSchemaDto>>.Success(_mapper.Map<IReadOnlyList<AccountingSchemaDto>>(items));
    }
}

public class GetSchemaByIdQueryHandler : IRequestHandler<GetSchemaByIdQuery, Result<AccountingSchemaDto>>
{
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IMapper _mapper;

    public GetSchemaByIdQueryHandler(IAccountingSchemaRepository schemas, IMapper mapper)
    {
        _schemas = schemas;
        _mapper = mapper;
    }

    public async Task<Result<AccountingSchemaDto>> Handle(GetSchemaByIdQuery request, CancellationToken cancellationToken)
    {
        var schema = await _schemas.Query().Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.SchemaId == request.SchemaId, cancellationToken);
        if (schema is null) return Result<AccountingSchemaDto>.Failure("SCHEMA_NOT_FOUND", "Schema not found.");
        return Result<AccountingSchemaDto>.Success(_mapper.Map<AccountingSchemaDto>(schema));
    }
}

public class GetMovementsQueryHandler : IRequestHandler<GetMovementsQuery, Result<PaginatedResult<MovementDto>>>
{
    private readonly IRepository<Movement> _movements;
    private readonly IMapper _mapper;

    public GetMovementsQueryHandler(IRepository<Movement> movements, IMapper mapper)
    {
        _movements = movements;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<MovementDto>>> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _movements.Query().AsQueryable();
        if (request.FromDate.HasValue)      query = query.Where(m => m.TransactionDate >= request.FromDate);
        if (request.ToDate.HasValue)        query = query.Where(m => m.TransactionDate <= request.ToDate);
        if (!string.IsNullOrEmpty(request.Account)) query = query.Where(m => m.Account == request.Account);
        if (request.TransactionId.HasValue) query = query.Where(m => m.TransactionId == request.TransactionId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.TransactionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<MovementDto>>(items);
        return Result<PaginatedResult<MovementDto>>.Success(new PaginatedResult<MovementDto>(dtos, request.Page, request.PageSize, total));
    }
}

public class GetMovementsByTransactionQueryHandler : IRequestHandler<GetMovementsByTransactionQuery, Result<IReadOnlyList<MovementDto>>>
{
    private readonly IRepository<Movement> _movements;
    private readonly IMapper _mapper;

    public GetMovementsByTransactionQueryHandler(IRepository<Movement> movements, IMapper mapper)
    {
        _movements = movements;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<MovementDto>>> Handle(GetMovementsByTransactionQuery request, CancellationToken cancellationToken)
    {
        var items = await _movements.Query()
            .Where(m => m.TransactionId == request.TransactionId)
            .OrderBy(m => m.LineOrder)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<MovementDto>>(items);
        return Result<IReadOnlyList<MovementDto>>.Success(dtos);
    }
}
