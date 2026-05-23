using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AggregatorPlatform.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AggregatorDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AggregatorDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
