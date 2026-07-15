using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IAccountingEngine
{
    Task ApplyAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
