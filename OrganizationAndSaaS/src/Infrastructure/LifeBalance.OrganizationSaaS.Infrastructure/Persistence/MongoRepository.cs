using System.Linq.Expressions;
using MongoDB.Driver;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Common;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;

namespace LifeBalance.OrganizationSaaS.Infrastructure.Persistence;

public class MongoRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ITenantContext _tenantContext;

    public MongoRepository(MongoDbContext dbContext, ITenantContext tenantContext)
    {
        _collection = dbContext.GetCollection<TEntity>();
        _tenantContext = tenantContext;
    }

    private FilterDefinition<TEntity> ApplyTenantFilter(FilterDefinition<TEntity> baseFilter)
    {
        var builder = Builders<TEntity>.Filter;
        var filters = new List<FilterDefinition<TEntity>> { baseFilter, builder.Eq(x => x.IsDeleted, false) };

        // Global catalog entities (e.g. SaaSPlan) are exempt from the tenant filter.
        if (!typeof(IGlobalTenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            // Fail-closed: an authenticated caller MUST resolve a tenant, otherwise queries would
            // silently cross tenant boundaries. Anonymous flows (e.g. invitation accept/reject,
            // internal provisioning) keep the previous unfiltered behavior.
            if (string.IsNullOrWhiteSpace(_tenantContext.TenantId))
            {
                if (_tenantContext.IsAuthenticated)
                {
                    throw new MultiTenantViolationException("A tenant context is required to access tenant-scoped resources.");
                }
            }
            else
            {
                filters.Add(builder.Eq(x => x.TenantId, _tenantContext.TenantId));
            }
        }

        return builder.And(filters);
    }

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Eq(x => x.Id, id));
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Empty);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Where(predicate));
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
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Where(predicate));
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
        if (string.IsNullOrWhiteSpace(entity.TenantId) && !string.IsNullOrWhiteSpace(_tenantContext.TenantId))
        {
            entity.SetTenantId(_tenantContext.TenantId);
        }

        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id));
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Eq(x => x.Id, id));
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
        var filter = ApplyTenantFilter(Builders<TEntity>.Filter.Where(predicate));
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
