using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Financial.Queries;

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<Result<TransactionDto>>;

public record GetTransactionsQuery(
    Guid? PartnerId,
    string? BankAccount,
    string? PhoneNumber,
    string? PartnerTransactionRef,
    TransactionStatus? Status,
    TransactionType? Type,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PaginatedResult<TransactionDto>>>;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly ITransactionRepository _txs;
    private readonly IMapper _mapper;

    public GetTransactionByIdQueryHandler(ITransactionRepository txs, IMapper mapper)
    {
        _txs = txs;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var tx = await _txs.GetByIdAsync(request.TransactionId, cancellationToken);
        if (tx is null) return Result<TransactionDto>.Failure("TRANSACTION_NOT_FOUND", "Transaction not found.");
        return Result<TransactionDto>.Success(_mapper.Map<TransactionDto>(tx));
    }
}

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, Result<PaginatedResult<TransactionDto>>>
{
    private readonly ITransactionRepository _txs;
    private readonly IMapper _mapper;

    public GetTransactionsQueryHandler(ITransactionRepository txs, IMapper mapper)
    {
        _txs = txs;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _txs.Query();
        if (request.PartnerId.HasValue) query = query.Where(x => x.PartnerId == request.PartnerId);
        if (!string.IsNullOrWhiteSpace(request.BankAccount))
            query = query.Where(x => x.BankAccount == request.BankAccount.Trim());
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            query = query.Where(x => x.PhoneNumber == request.PhoneNumber.Trim());
        if (!string.IsNullOrWhiteSpace(request.PartnerTransactionRef))
        {
            var term = request.PartnerTransactionRef.Trim();
            query = query.Where(x => x.PartnerTransactionRef.Contains(term));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.Type.HasValue) query = query.Where(x => x.TransactionType == request.Type);
        if (request.FromDate.HasValue) query = query.Where(x => x.InitiatedAt >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(x => x.InitiatedAt <= request.ToDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.InitiatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<TransactionDto>>(items);
        return Result<PaginatedResult<TransactionDto>>.Success(new PaginatedResult<TransactionDto>(dtos, request.Page, request.PageSize, total));
    }
}
