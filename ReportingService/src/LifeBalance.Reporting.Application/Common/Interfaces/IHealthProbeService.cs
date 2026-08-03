namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Probes the health of the upstream microservices and reports platform health.
/// </summary>
public interface IHealthProbeService
{
    /// <summary>
    /// Returns the percentage (0..100) of upstream services reporting a healthy status.
    /// Throws <c>UpstreamServiceUnavailableException</c> when no probe could be performed.
    /// </summary>
    Task<double> GetPlatformHealthPercentageAsync(CancellationToken cancellationToken = default);
}
