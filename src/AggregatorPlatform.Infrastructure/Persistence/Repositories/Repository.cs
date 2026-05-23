using System.Linq.Expressions;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AggregatorDbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(AggregatorDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Set.FindAsync(new[] { id }, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await Set.Where(predicate).ToListAsync(cancellationToken);

    public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.AnyAsync(predicate, cancellationToken);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null ? Set.CountAsync(cancellationToken) : Set.CountAsync(predicate, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await Set.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => Set.Update(entity);
    public virtual void Remove(T entity) => Set.Remove(entity);
    public virtual IQueryable<T> Query() => Set.AsQueryable();
}
