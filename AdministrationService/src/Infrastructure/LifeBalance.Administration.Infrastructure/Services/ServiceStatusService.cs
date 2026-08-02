using System.Text.Json;
using Microsoft.Extensions.Logging;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Infrastructure.Services;

/// <summary>
/// Probes every upstream microservice in parallel, persists a <see cref="ServiceStatus"/>
/// snapshot per service and caches the board for a short window.
/// </summary>
public class ServiceStatusService : IServiceStatusService
{
    private const string BoardCacheKey = "admin:services:board";
    private static readonly TimeSpan BoardCacheDuration = TimeSpan.FromSeconds(30);

    private readonly IAuthProfileServiceClient _auth;
    private readonly IOrganizationServiceClient _organization;
    private readonly INotificationServiceClient _notifications;
    private readonly IMedicalDataServiceClient _medical;
    private readonly ISedentaryEngineServiceClient _sedentary;
    private readonly IDashboardServiceClient _dashboard;
    private readonly IReportingServiceClient _reporting;
    private readonly IMLPredictionServiceClient _ml;
    private readonly IRepository<ServiceStatus> _statusRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<ServiceStatusService> _logger;

    public ServiceStatusService(
        IAuthProfileServiceClient auth,
        IOrganizationServiceClient organization,
        INotificationServiceClient notifications,
        IMedicalDataServiceClient medical,
        ISedentaryEngineServiceClient sedentary,
        IDashboardServiceClient dashboard,
        IReportingServiceClient reporting,
        IMLPredictionServiceClient ml,
        IRepository<ServiceStatus> statusRepository,
        ICacheService cache,
        ILogger<ServiceStatusService> logger)
    {
        _auth = auth;
        _organization = organization;
        _notifications = notifications;
        _medical = medical;
        _sedentary = sedentary;
        _dashboard = dashboard;
        _reporting = reporting;
        _ml = ml;
        _statusRepository = statusRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServiceStatusSnapshot>> GetBoardAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh)
        {
            var cached = await _cache.GetAsync<IReadOnlyList<ServiceStatusSnapshot>>(BoardCacheKey, cancellationToken);
            if (cached is { Count: > 0 }) return cached;
        }

        var probes = new List<ServiceProbe>
        {
            new(MicroserviceName.Auth, "Auth & Profile", _auth.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.Organization, "Organization & SaaS", _organization.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.Notifications, "Notifications & Alerts", _notifications.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.MedicalData, "Medical Data", _medical.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.SedentaryEngine, "Sedentary Engine", _sedentary.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.Dashboard, "Dashboard", _dashboard.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.Reporting, "Reporting", _reporting.GetStatusAsync(cancellationToken)),
            new(MicroserviceName.MlPrediction, "ML Prediction", _ml.GetStatusAsync(cancellationToken))
        };

        await Task.WhenAll(probes.Select(p => p.Result));

        var checkedAt = DateTime.UtcNow;
        var snapshots = new List<ServiceStatusSnapshot>(probes.Count);
        foreach (var probe in probes)
        {
            var result = await probe.Result;
            var status = result.IsHealthy ? ServiceHealthStatus.Healthy : ServiceHealthStatus.Unhealthy;
            var snapshot = new ServiceStatusSnapshot(
                probe.Service,
                probe.Label,
                status,
                result.StatusCode,
                result.Message,
                result.LatencyMs,
                result.Version,
                result.Payload,
                checkedAt,
                null);

            snapshots.Add(snapshot);
            await PersistAsync(probe.Service, probe.Label, status, result, checkedAt, cancellationToken);
        }

        await _cache.SetAsync(BoardCacheKey, snapshots, BoardCacheDuration, cancellationToken);
        return snapshots;
    }

    public async Task<ServiceStatusSnapshot> GetServiceAsync(MicroserviceName service, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var probe = CreateProbe(service, cancellationToken);
        var result = await probe.Result;

        object? payload = result.Payload;
        if (result.IsHealthy)
        {
            var enrichment = await GetEnrichmentAsync(service, cancellationToken);
            payload = enrichment ?? payload;
        }

        var checkedAt = DateTime.UtcNow;
        var status = result.IsHealthy ? ServiceHealthStatus.Healthy : ServiceHealthStatus.Unhealthy;
        var snapshot = new ServiceStatusSnapshot(
            probe.Service,
            probe.Label,
            status,
            result.StatusCode,
            result.Message,
            result.LatencyMs,
            result.Version,
            payload,
            checkedAt,
            null);

        await PersistAsync(probe.Service, probe.Label, status, result, checkedAt, cancellationToken);
        return snapshot;
    }

    private ServiceProbe CreateProbe(MicroserviceName service, CancellationToken cancellationToken)
        => service switch
        {
            MicroserviceName.Auth => new(MicroserviceName.Auth, "Auth & Profile", _auth.GetStatusAsync(cancellationToken)),
            MicroserviceName.Organization => new(MicroserviceName.Organization, "Organization & SaaS", _organization.GetStatusAsync(cancellationToken)),
            MicroserviceName.Notifications => new(MicroserviceName.Notifications, "Notifications & Alerts", _notifications.GetStatusAsync(cancellationToken)),
            MicroserviceName.MedicalData => new(MicroserviceName.MedicalData, "Medical Data", _medical.GetStatusAsync(cancellationToken)),
            MicroserviceName.SedentaryEngine => new(MicroserviceName.SedentaryEngine, "Sedentary Engine", _sedentary.GetStatusAsync(cancellationToken)),
            MicroserviceName.Dashboard => new(MicroserviceName.Dashboard, "Dashboard", _dashboard.GetStatusAsync(cancellationToken)),
            MicroserviceName.Reporting => new(MicroserviceName.Reporting, "Reporting", _reporting.GetStatusAsync(cancellationToken)),
            _ => new(MicroserviceName.MlPrediction, "ML Prediction", _ml.GetStatusAsync(cancellationToken))
        };

    private async Task<object?> GetEnrichmentAsync(MicroserviceName service, CancellationToken cancellationToken)
        => service switch
        {
            MicroserviceName.Auth => await _auth.GetUsersAsync(cancellationToken),
            MicroserviceName.Organization => await _organization.GetOrganizationsAsync(cancellationToken),
            MicroserviceName.Notifications => await _notifications.GetNotificationHistoryAsync(cancellationToken),
            MicroserviceName.MedicalData => await _medical.GetStatisticsAsync(cancellationToken),
            MicroserviceName.SedentaryEngine => await _sedentary.GetMetricsAsync(cancellationToken),
            MicroserviceName.Dashboard => await _dashboard.GetKpisAsync(cancellationToken),
            MicroserviceName.Reporting => await _reporting.GetReportsAsync(cancellationToken),
            MicroserviceName.MlPrediction => await _ml.GetModelStatusAsync(cancellationToken),
            _ => null
        };

    private async Task PersistAsync(
        MicroserviceName service,
        string label,
        ServiceHealthStatus status,
        ServiceHealthResult result,
        DateTime checkedAt,
        CancellationToken cancellationToken)
    {
        var existing = (await _statusRepository.FindAsync(x => x.Service == service, cancellationToken)).FirstOrDefault();
        if (existing == null)
        {
            existing = new ServiceStatus(service, label);
            existing.Report(status, result.StatusCode, result.Message, result.LatencyMs, result.Version, SerializePayload(result.Payload), checkedAt);
            await _statusRepository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.Report(status, result.StatusCode, result.Message, result.LatencyMs, result.Version, SerializePayload(result.Payload), checkedAt);
            await _statusRepository.UpdateAsync(existing, cancellationToken);
        }

        _logger.LogInformation("Service {Service} status updated to {Status} (latency {LatencyMs} ms)",
            label, status, result.LatencyMs);
    }

    private static string? SerializePayload(object? payload)
    {
        if (payload == null) return null;
        try
        {
            return JsonSerializer.Serialize(payload);
        }
        catch (Exception ex)
        {
            // Never let a serialization failure break the monitoring board.
            return "{\"error\":\"" + System.Security.SecurityElement.Escape(ex.Message) + "\"}";
        }
    }

    private sealed record ServiceProbe(MicroserviceName Service, string Label, Task<ServiceHealthResult> Result);
}
