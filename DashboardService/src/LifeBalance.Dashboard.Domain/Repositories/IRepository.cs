namespace LifeBalance.Dashboard.Domain.Repositories;

/// <summary>
/// Generic repository interface for Aggregate Roots.
/// Implementations belong to the Infrastructure layer.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IRepository<TAggregate, TId>
    where TAggregate : class
    where TId : notnull
{
    /// <summary>Finds an aggregate by its identifier.</summary>
    Task<TAggregate?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new aggregate.</summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing aggregate.</summary>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>Deletes an aggregate by its identifier.</summary>
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an aggregate with the given Id exists.</summary>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
