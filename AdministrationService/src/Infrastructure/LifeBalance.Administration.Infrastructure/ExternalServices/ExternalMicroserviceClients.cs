using Microsoft.Extensions.Logging;
using LifeBalance.Administration.Application.Interfaces;

namespace LifeBalance.Administration.Infrastructure.ExternalServices;

/// <summary>Client for the Auth &amp; Profile Service.</summary>
public class AuthProfileServiceClient : BaseServiceClient, IAuthProfileServiceClient
{
    public AuthProfileServiceClient(HttpClient httpClient, ILogger<AuthProfileServiceClient> logger)
        : base(httpClient, logger, "Auth") { }

    public Task<object?> GetUsersAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/users", cancellationToken);

    public Task<IReadOnlyList<AuthRoleDto>?> GetRolesAsync(CancellationToken cancellationToken = default)
        => TryGetListAsync<AuthRoleDto>("/api/v1/roles", cancellationToken);

    public Task<IReadOnlyList<AuthPermissionDto>?> GetPermissionsAsync(CancellationToken cancellationToken = default)
        => TryGetListAsync<AuthPermissionDto>("/api/v1/permissions", cancellationToken);

    public Task<object?> GetAdministratorsAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/users?role=SUPERADMIN", cancellationToken);
}

/// <summary>Client for the Organization &amp; SaaS Service.</summary>
public class OrganizationServiceClient : BaseServiceClient, IOrganizationServiceClient
{
    public OrganizationServiceClient(HttpClient httpClient, ILogger<OrganizationServiceClient> logger)
        : base(httpClient, logger, "Organization") { }

    public Task<IReadOnlyList<OrganizationInfoDto>?> GetOrganizationsAsync(CancellationToken cancellationToken = default)
        => TryGetListAsync<OrganizationInfoDto>("/api/v1/organizations", cancellationToken);

    public Task<IReadOnlyList<OrganizationLicenseDto>?> GetLicensesAsync(CancellationToken cancellationToken = default)
        => TryGetListAsync<OrganizationLicenseDto>("/api/v1/licenses", cancellationToken);
}

/// <summary>Client for the Notifications &amp; Alerts Service.</summary>
public class NotificationServiceClient : BaseServiceClient, INotificationServiceClient
{
    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
        : base(httpClient, logger, "Notifications") { }

    public Task<object?> GetNotificationHistoryAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/notifications", cancellationToken);
}

/// <summary>Client for the Medical Data Service.</summary>
public class MedicalDataServiceClient : BaseServiceClient, IMedicalDataServiceClient
{
    public MedicalDataServiceClient(HttpClient httpClient, ILogger<MedicalDataServiceClient> logger)
        : base(httpClient, logger, "MedicalData") { }

    public Task<object?> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/statistics", cancellationToken);
}

/// <summary>Client for the Sedentary Engine Service.</summary>
public class SedentaryEngineServiceClient : BaseServiceClient, ISedentaryEngineServiceClient
{
    public SedentaryEngineServiceClient(HttpClient httpClient, ILogger<SedentaryEngineServiceClient> logger)
        : base(httpClient, logger, "SedentaryEngine") { }

    public Task<object?> GetMetricsAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/metrics/summary", cancellationToken);

    public Task<object?> GetConfigurationAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/config", cancellationToken);
}

/// <summary>Client for the Dashboard Service.</summary>
public class DashboardServiceClient : BaseServiceClient, IDashboardServiceClient
{
    public DashboardServiceClient(HttpClient httpClient, ILogger<DashboardServiceClient> logger)
        : base(httpClient, logger, "Dashboard")
    {
    }

    protected override string HealthPath => "/health/live";

    public Task<object?> GetKpisAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/dashboard/kpis", cancellationToken);
}

/// <summary>Client for the Reporting Service.</summary>
public class ReportingServiceClient : BaseServiceClient, IReportingServiceClient
{
    public ReportingServiceClient(HttpClient httpClient, ILogger<ReportingServiceClient> logger)
        : base(httpClient, logger, "Reporting") { }

    public Task<object?> GetReportsAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/reports", cancellationToken);
}

/// <summary>Client for the ML Prediction Service.</summary>
public class MLPredictionServiceClient : BaseServiceClient, IMLPredictionServiceClient
{
    public MLPredictionServiceClient(HttpClient httpClient, ILogger<MLPredictionServiceClient> logger)
        : base(httpClient, logger, "MlPrediction") { }

    public Task<object?> GetModelStatusAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/ml/model/status", cancellationToken);

    public Task<object?> GetAiConfigurationAsync(CancellationToken cancellationToken = default)
        => TryGetJsonAsync("/api/v1/ml/config", cancellationToken);
}
