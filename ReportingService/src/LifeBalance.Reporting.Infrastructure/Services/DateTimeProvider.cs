using LifeBalance.Reporting.Application.Common.Interfaces;

namespace LifeBalance.Reporting.Infrastructure.Services;

/// <summary>
/// Returns the current UTC time from the system clock.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}
