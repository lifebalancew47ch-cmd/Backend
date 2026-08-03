namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Abstracción over the current UTC clock so that tests can substitute a fixed time.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTime UtcNow { get; }
}
