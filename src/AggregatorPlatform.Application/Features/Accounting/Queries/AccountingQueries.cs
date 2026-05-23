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
public record GetJournalEntriesQuery(DateTime? FromDate, DateTime? ToDate, int Page = 1, int PageSize = 50)
    : IRequest<Result<PaginatedResult<JournalEntryDto>>>;

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

public class GetJournalEntriesQueryHandler : IRequestHandler<GetJournalEntriesQuery, Result<PaginatedResult<JournalEntryDto>>>
{
    private readonly IRepository<JournalEntry> _entries;
    private readonly IMapper _mapper;

    public GetJournalEntriesQueryHandler(IRepository<JournalEntry> entries, IMapper mapper)
    {
        _entries = entries;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<JournalEntryDto>>> Handle(GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _entries.Query().Include(e => e.Lines).AsQueryable();
        if (request.FromDate.HasValue) query = query.Where(e => e.EntryDate >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(e => e.EntryDate <= request.ToDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.EntryDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<JournalEntryDto>>(items);
        return Result<PaginatedResult<JournalEntryDto>>.Success(new PaginatedResult<JournalEntryDto>(dtos, request.Page, request.PageSize, total));
    }
}
