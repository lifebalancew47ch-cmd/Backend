namespace LifeBalance.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Abstraction for date and time retrieval.
/// Inject this instead of <c>DateTime.UtcNow</c> to enable deterministic unit testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTime UtcNow { get; }
}
