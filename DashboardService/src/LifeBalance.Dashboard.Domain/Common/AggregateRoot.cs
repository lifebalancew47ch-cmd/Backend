namespace LifeBalance.Dashboard.Domain.Common;

/// <summary>
/// Base class for Aggregate Roots.
/// An aggregate root is the entry point to an aggregate cluster of entities.
/// Only aggregate roots may be loaded from the repository.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>Gets the concurrency version for optimistic locking.</summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Increments the aggregate version. Called after persisting a new event.
    /// </summary>
    protected void IncrementVersion() => Version++;
}
