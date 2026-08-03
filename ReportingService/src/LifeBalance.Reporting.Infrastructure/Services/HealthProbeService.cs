using System.Diagnostics;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.Services;

/// <summary>
/// Probes the health endpoints of the upstream microservices and reports the
/// percentage of healthy services.
/// </summary>
public sealed class HealthProbeService : IHealthProbeService
{
    private readonly IAuthServiceClient _authClient;
    private readonly IMedicalDataServiceClient _medicalClient;
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IDashboardServiceClient _dashboardClient;
    private readonly IOrganizationServiceClient _organizationClient;
    private readonly ILogger<HealthProbeService> _logger;

    /// <summary>Initializes a new instance of <see cref="HealthProbeService"/>.</summary>
    public HealthProbeService(
        IAuthServiceClient authClient,
        IMedicalDataServiceClient medicalClient,
        ISedentaryEngineServiceClient sedentaryClient,
        IDashboardServiceClient dashboardClient,
        IOrganizationServiceClient organizationClient,
        ILogger<HealthProbeService> logger)
    {
        _authClient = authClient;
        _medicalClient = medicalClient;
        _sedentaryClient = sedentaryClient;
        _dashboardClient = dashboardClient;
        _organizationClient = organizationClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<double> GetPlatformHealthPercentageAsync(CancellationToken cancellationToken = default)
    {
        // Each probe call runs with a strict timeout so an unresponsive upstream
        // cannot stall the health calculation.
        var probes = new List<Task<bool>>
        {
            ProbeAsync(() => IsHealthyAsync(_authClient.GetUserProfileAsync("probe-user", cancellationToken)), "Auth"),
            ProbeAsync(() => IsHealthyAsync(_medicalClient.GetDailyActiveUsersAsync(cancellationToken)), "Medical"),
            ProbeAsync(() => IsHealthyAsync(_sedentaryClient.GetUserGoalsAsync("probe-user", cancellationToken)), "Sedentary"),
            ProbeAsync(() => IsHealthyAsync(_dashboardClient.GetDashboardSummaryAsync("individual", null, cancellationToken)), "Dashboard"),
            ProbeAsync(() => IsHealthyAsync(_organizationClient.GetPlatformStatsAsync(cancellationToken)), "Organization")
        };

        var results = await Task.WhenAll(probes);
        var healthy = results.Count(r => r);

        var percentage = (healthy / (double)results.Length) * 100.0;
        _logger.LogInformation("Platform health probe: {Healthy}/{Total} services healthy ({Percentage:0.#}%).",
            healthy, results.Length, percentage);

        return percentage;
    }

    private static async Task<bool> IsHealthyAsync<T>(Task<T?> probe)
    {
        var result = await probe;
        return result is not null;
    }

    private async Task<bool> ProbeAsync(Func<Task<bool>> probe, string serviceName)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var healthy = await probe().WaitAsync(TimeSpan.FromSeconds(3));
            _logger.LogDebug("Probe {ServiceName} {Status} in {ElapsedMs}ms.",
                serviceName, healthy ? "succeeded" : "returned no data", stopwatch.ElapsedMilliseconds);
            return healthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Probe {ServiceName} failed in {ElapsedMs}ms.", serviceName, stopwatch.ElapsedMilliseconds);
            return false;
        }
    }
}
