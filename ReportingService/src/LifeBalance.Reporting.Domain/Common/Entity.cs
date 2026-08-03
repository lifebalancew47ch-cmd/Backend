namespace LifeBalance.Reporting.Domain.Common;

/// <summary>
/// Base class for all Domain Entities.
/// Enforces identity equality based on the entity's Id.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Gets the entity identifier.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>Gets the UTC date and time when the entity was created.</summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC date and time of the last update.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>Gets the read-only list of pending domain events.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Raises a domain event by adding it to the pending list.</summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears all pending domain events after they have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);
}
