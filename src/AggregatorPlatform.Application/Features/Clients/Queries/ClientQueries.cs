using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Clients.Queries;

/// <summary>Liste des Clients (racines) avec le nombre de Customers rattaches.</summary>
public record GetAllClientsQuery(int? Take = 500) : IRequest<Result<IReadOnlyList<ClientDto>>>;

public class GetAllClientsQueryHandler : IRequestHandler<GetAllClientsQuery, Result<IReadOnlyList<ClientDto>>>
{
    private readonly IRepository<Client> _clients;

    public GetAllClientsQueryHandler(IRepository<Client> clients) => _clients = clients;

    public async Task<Result<IReadOnlyList<ClientDto>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take ?? 500;
        var list = await _clients.Query()
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .Select(c => new ClientDto(
                c.ClientId,
                c.BankAccountRoot,
                c.FullName,
                c.DateOfBirth,
                c.NationalId,
                c.PhoneNumber,
                c.Email,
                c.Customers.Count,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ClientDto>>.Success(list);
    }
}

/// <summary>Detail d'un Client + ses Customers rattaches.</summary>
public record GetClientByIdQuery(Guid ClientId) : IRequest<Result<ClientDetailDto>>;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, Result<ClientDetailDto>>
{
    private readonly IRepository<Client> _clients;

    public GetClientByIdQueryHandler(IRepository<Client> clients) => _clients = clients;

    public async Task<Result<ClientDetailDto>> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _clients.Query()
            .Include(c => c.Customers)
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId, cancellationToken);

        if (client is null)
            return Result<ClientDetailDto>.Failure("CLIENT_NOT_FOUND", "Client not found.");

        var customers = client.Customers
            .Select(cu => new ClientCustomerDto(
                cu.CustomerId, cu.FullName, cu.NationalId, cu.Email,
                (int)cu.Status, (int)cu.KycStatus, cu.CreatedAt))
            .ToList();

        return Result<ClientDetailDto>.Success(new ClientDetailDto(
            client.ClientId, client.BankAccountRoot, client.FullName,
            client.DateOfBirth, client.NationalId, client.PhoneNumber, client.Email,
            client.CreatedAt, customers));
    }
}
