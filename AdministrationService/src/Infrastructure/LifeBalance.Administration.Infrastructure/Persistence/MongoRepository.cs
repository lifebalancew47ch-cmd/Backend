using System.Linq.Expressions;
using MongoDB.Driver;
using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Infrastructure.Persistence;

/// <summary>
/// MongoDB repository implementing the generic <see cref="IRepository{TEntity}"/>.
/// Always filters out soft-deleted records. This is a global administration
/// service, so no tenant isolation is applied.
/// </summary>
public class MongoRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly IMongoCollection<TEntity> _collection;

    public MongoRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<TEntity>();
    }

    private static FilterDefinition<TEntity> ApplyFilter(FilterDefinition<TEntity> baseFilter)
        => Builders<TEntity>.Filter.And(baseFilter, Builders<TEntity>.Filter.Eq(x => x.IsDeleted, false));

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Eq(x => x.Id, id));
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Empty);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Where(predicate));
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Where(predicate));
        var countTask = _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var query = _collection.Find(filter);

        if (orderBy != null)
        {
            query = sortDescending
                ? query.SortByDescending(orderBy)
                : query.SortBy(orderBy);
        }

        var skip = (pageIndex - 1) * pageSize;
        var itemsTask = query.Skip(skip).Limit(pageSize).ToListAsync(cancellationToken);

        await Task.WhenAll(countTask, itemsTask);

        return (itemsTask.Result, countTask.Result);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        var filter = ApplyFilter(Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id));
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Eq(x => x.Id, id));
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.SoftDelete();
            await UpdateAsync(entity, cancellationToken);
        }
    }

    public async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = ApplyFilter(Builders<TEntity>.Filter.Where(predicate));
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
