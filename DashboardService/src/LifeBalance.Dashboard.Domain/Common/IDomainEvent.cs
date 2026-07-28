namespace LifeBalance.Dashboard.Domain.Common;

/// <summary>
/// Marker interface for all Domain Events in the LifeBalance Dashboard bounded context.
/// All domain events must implement this interface and <see cref="INotification"/>
/// to participate in the MediatR in-process event pipeline.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Gets the unique event identifier.</summary>
    Guid EventId { get; }

    /// <summary>Gets the UTC date and time when the event occurred.</summary>
    DateTime OccurredAt { get; }
}
