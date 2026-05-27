using AggregatorPlatform.API.Filters;
using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Customers.Commands;
using AggregatorPlatform.Application.Features.Customers.Queries;
using AggregatorPlatform.Application.Features.Subscriptions.Commands;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

[Route("api/v1/customers")]
//[RequirePartner]
public class CustomerController : BaseApiController
{
    private readonly ICurrentPartnerService _currentPartner;

    public CustomerController(ICurrentPartnerService currentPartner) => _currentPartner = currentPartner;

    /// <summary>List all customers.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerDto>>>> List(CancellationToken ct)
        => ToResponse(await Mediator.Send(new ListCustomersQuery(), ct));

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new CreateCustomerCommand(request), ct));

    /// <summary>Get a customer by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetCustomerByIdQuery(id), ct));

    /// <summary>Update a customer.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
        => ToResponse(await Mediator.Send(new UpdateCustomerCommand(id, request), ct));

    /// <summary>Create a subscription for a customer.</summary>
    [HttpPost("{id:guid}/subscriptions")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSubscription(Guid id, [FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        var partnerId = _currentPartner.PartnerId!.Value;
        return ToResponse(await Mediator.Send(new CreateSubscriptionCommand(id, partnerId, request), ct));
    }

    /// <summary>List subscriptions of a customer.</summary>
    [HttpGet("{id:guid}/subscriptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionDto>>>> GetSubscriptions(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetCustomerSubscriptionsQuery(id), ct));
}
