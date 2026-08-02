using System.Linq.Expressions;
using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Interfaces;

/// <summary>
/// Generic repository contract used by the application layer. Every filter is
/// applied on top of an "IsDeleted == false" guard, so soft-deleted records are
/// never returned to callers.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}
