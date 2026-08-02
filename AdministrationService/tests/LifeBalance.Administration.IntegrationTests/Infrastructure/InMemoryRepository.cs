using System.Collections.Concurrent;
using System.Linq.Expressions;
using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory IRepository implementation used by integration tests so handlers run
/// against a deterministic store instead of a real MongoDB instance.
/// </summary>
public class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, TEntity>> Stores = new();

    private static ConcurrentDictionary<string, TEntity> Store
        => Stores.GetOrAdd(typeof(TEntity), _ => new ConcurrentDictionary<string, TEntity>());

    public static void Reset() => Stores.TryRemove(typeof(TEntity), out _);

    public Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        Store.TryGetValue(id, out var entity);
        return Task.FromResult(entity is { IsDeleted: false } ? entity : null);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = Store.Values.Where(x => !x.IsDeleted).ToList();
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var result = Store.Values.Where(x => !x.IsDeleted && compiled(x)).ToList();
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        IQueryable<TEntity> query = Store.Values.Where(x => !x.IsDeleted && compiled(x)).AsQueryable();

        if (orderBy != null)
        {
            query = sortDescending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }

        var total = query.Count();
        var items = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<TEntity>)items, (long)total));
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Store.TryAdd(entity.Id, entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        Store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (Store.TryGetValue(id, out var entity))
        {
            entity.SoftDelete();
            Store[id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult(Store.Values.LongCount(x => !x.IsDeleted && compiled(x)));
    }
}
