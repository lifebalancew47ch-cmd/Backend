namespace LifeBalance.Dashboard.Domain.Common;

/// <summary>
/// Base record for Domain Events.
/// Provides default implementations of <see cref="IDomainEvent"/>.
/// </summary>
/// <param name="EventId">Unique event identifier (auto-generated).</param>
/// <param name="OccurredAt">UTC timestamp when the event occurred (auto-generated).</param>
public abstract record DomainEvent(
    Guid EventId,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Initializes a new domain event with auto-generated Id and timestamp.
    /// </summary>
    protected DomainEvent()
        : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
