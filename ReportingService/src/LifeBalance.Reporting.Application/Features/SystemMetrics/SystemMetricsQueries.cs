using System.Reflection;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.SystemMetrics;

public sealed record GetSystemMetricsQuery : IRequest<Result<GeneralSystemMetricsDto>>;

/// <summary>
/// Platform-wide metrics consumed by the Dashboard microservice.
/// </summary>
public sealed record GeneralSystemMetricsDto(
    int TotalUsers,
    int ActiveUsersToday,
    double PlatformHealthPercentage,
    string SystemVersion);

/// <summary>
/// Aggregates real platform metrics from the upstream services. Fails closed (503)
/// when a required upstream service is unavailable; never fabricates data.
/// </summary>
public sealed class GetSystemMetricsQueryHandler : IRequestHandler<GetSystemMetricsQuery, Result<GeneralSystemMetricsDto>>
{
    private readonly IHealthProbeService _healthProbeService;
    private readonly IOrganizationServiceClient _organizationClient;
    private readonly IMedicalDataServiceClient _medicalClient;
    private readonly IReportGenerationLogService _logService;

    public GetSystemMetricsQueryHandler(
        IHealthProbeService healthProbeService,
        IOrganizationServiceClient organizationClient,
        IMedicalDataServiceClient medicalClient,
        IReportGenerationLogService logService)
    {
        _healthProbeService = healthProbeService;
        _organizationClient = organizationClient;
        _medicalClient = medicalClient;
        _logService = logService;
    }

    public async Task<Result<GeneralSystemMetricsDto>> Handle(
        GetSystemMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var healthTask = _healthProbeService.GetPlatformHealthPercentageAsync(cancellationToken);
        var statsTask = _organizationClient.GetPlatformStatsAsync(cancellationToken);
        var activeTask = _medicalClient.GetDailyActiveUsersAsync(cancellationToken);

        await Task.WhenAll(healthTask, statsTask, activeTask);

        var healthPercentage = await healthTask;
        var stats = await statsTask
            ?? throw new UpstreamServiceUnavailableException("Platform statistics are unavailable from the Organization service.");
        var active = await activeTask
            ?? throw new UpstreamServiceUnavailableException("Daily active users are unavailable from the Medical Data service.");

        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "1.0.0";

        var response = new GeneralSystemMetricsDto(
            TotalUsers: stats.TotalUsers,
            ActiveUsersToday: active.ActiveUsersToday,
            PlatformHealthPercentage: Math.Round(healthPercentage, 1),
            SystemVersion: version);

        await _logService.LogAsync(
            ReportScope.Individual,
            null,
            "system",
            null,
            ReportStatus.Completed,
            0,
            0,
            correlationId: null,
            cancellationToken: cancellationToken);

        return Result.Success(response);
    }
}
