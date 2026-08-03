namespace LifeBalance.Reporting.Domain.Common;

/// <summary>
/// Base record for a domain event. Carries the UTC timestamp of when it occurred.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
