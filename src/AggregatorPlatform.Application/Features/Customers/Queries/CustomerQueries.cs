using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AggregatorPlatform.Application.Features.Customers.Queries;

public record ListCustomersQuery : IRequest<Result<IReadOnlyList<CustomerDto>>>;
public record GetCustomerByIdQuery(Guid CustomerId) : IRequest<Result<CustomerDto>>;
public record GetCustomerSubscriptionsQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<SubscriptionDto>>>;

public class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, Result<IReadOnlyList<CustomerDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly IMapper _mapper;

    public ListCustomersQueryHandler(ICustomerRepository customers, IMapper mapper)
    {
        _customers = customers;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var list = await _customers.GetAllAsync(cancellationToken);
        var ordered = list.OrderByDescending(c => c.CreatedAt).ToList();
        return Result<IReadOnlyList<CustomerDto>>.Success(_mapper.Map<IReadOnlyList<CustomerDto>>(ordered));
    }
}

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IMapper _mapper;

    public GetCustomerByIdQueryHandler(ICustomerRepository customers, IMapper mapper)
    {
        _customers = customers;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null) return Result<CustomerDto>.Failure("CUSTOMER_NOT_FOUND", "Customer not found.");
        return Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }
}

public class GetCustomerSubscriptionsQueryHandler : IRequestHandler<GetCustomerSubscriptionsQuery, Result<IReadOnlyList<SubscriptionDto>>>
{
    private readonly ISubscriptionRepository _subs;
    private readonly IMapper _mapper;

    public GetCustomerSubscriptionsQueryHandler(ISubscriptionRepository subs, IMapper mapper)
    {
        _subs = subs;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> Handle(GetCustomerSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _subs.GetByCustomerAsync(request.CustomerId, cancellationToken);
        return Result<IReadOnlyList<SubscriptionDto>>.Success(_mapper.Map<IReadOnlyList<SubscriptionDto>>(list));
    }
}
