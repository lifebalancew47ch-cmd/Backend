using LifeBalance.Dashboard.Application.Common.Interfaces;

namespace LifeBalance.Dashboard.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IDateTimeProvider"/>.
/// Returns <c>DateTime.UtcNow</c> in production; can be replaced with a fixed clock in tests.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}
