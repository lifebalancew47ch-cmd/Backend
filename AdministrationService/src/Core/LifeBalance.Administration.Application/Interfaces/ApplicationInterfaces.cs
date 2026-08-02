using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Application.Interfaces;

/// <summary>
/// Accessor of the current administrative user context, extracted from the JWT
/// and the HTTP request (used for audit trails and traceability).
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserEmail { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
    string? RequestId { get; }
    bool IsAuthenticated { get; }
    bool IsAdministrator { get; }
}

/// <summary>
/// DTO used to write an audit entry without leaking domain construction details.
/// </summary>
public record AuditEntryDto(
    string UserId,
    string UserEmail,
    string Action,
    string EntityName,
    string EntityId,
    AuditOperationType OperationType,
    AuditEventType EventType,
    string Service,
    string Endpoint,
    string IpAddress,
    string UserAgent,
    string CorrelationId,
    string RequestId,
    bool Result = true,
    string? DetailsJson = null,
    string? OrganizationId = null,
    string? CompanyId = null);

/// <summary>Audit trail writer.</summary>
public interface IAuditService
{
    Task RecordAsync(AuditEntryDto entry, CancellationToken cancellationToken = default);
    Task RecordAsync(IEnumerable<AuditEntryDto> entries, CancellationToken cancellationToken = default);
}

/// <summary>Simple caching abstraction (in-memory distributed cache in prod).</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Result of a health probe against an upstream microservice.</summary>
public record ServiceHealthResult(
    bool IsHealthy,
    int? StatusCode,
    string Message,
    long LatencyMs,
    string? Version = null,
    object? Payload = null);

/// <summary>Public snapshot of a monitored microservice.</summary>
public record ServiceStatusSnapshot(
    MicroserviceName Service,
    string ServiceName,
    ServiceHealthStatus Status,
    int? StatusCode,
    string Message,
    long LatencyMs,
    string? Version,
    object? Payload,
    DateTime LastCheckedAt,
    DateTime? LastSuccessAt);

/// <summary>Aggregate summary of the monitoring board.</summary>
public record ServicesBoardSummary(
    int Total,
    int Healthy,
    int Degraded,
    int Unhealthy,
    int Unknown,
    DateTime LastCheckedAt);

// ─────────────────────────────────────────────────────────────────────────
// External microservice clients (HttpClientFactory + Polly resilience).
// Data enrichment methods are best-effort: they return null on failure so the
// monitoring board can still report the health status of the upstream.
// Handlers consuming these methods for first-class data (roles, permissions,
// organizational configuration) apply the fail-closed policy (null => 503).
// ─────────────────────────────────────────────────────────────────────────

/// <summary>Role exposed by the Auth &amp; Profile service.</summary>
public record AuthRoleDto(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionIds,
    DateTime? CreatedAt);

/// <summary>Permission exposed by the Auth &amp; Profile service.</summary>
public record AuthPermissionDto(
    string Id,
    string Name,
    string? Description,
    string Module,
    DateTime? CreatedAt);

/// <summary>Organization exposed by the Organization &amp; SaaS service.</summary>
public record OrganizationInfoDto(
    string Id,
    string Name,
    string Status,
    string? PlanId,
    string? TenantId,
    DateTime? CreatedAt);

/// <summary>License exposed by the Organization &amp; SaaS service.</summary>
public record OrganizationLicenseDto(
    string Id,
    string OrganizationId,
    string LicenseKey,
    string Type,
    string Status,
    string? AssignedUserId,
    DateTime? IssuedAt,
    DateTime? ExpiresAt);

public interface IAuthProfileServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthRoleDto>?> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthPermissionDto>?> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<object?> GetAdministratorsAsync(CancellationToken cancellationToken = default);
}

public interface IOrganizationServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationInfoDto>?> GetOrganizationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationLicenseDto>?> GetLicensesAsync(CancellationToken cancellationToken = default);
}

public interface INotificationServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetNotificationHistoryAsync(CancellationToken cancellationToken = default);
}

public interface IMedicalDataServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public interface ISedentaryEngineServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<object?> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

public interface IDashboardServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetKpisAsync(CancellationToken cancellationToken = default);
}

public interface IReportingServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetReportsAsync(CancellationToken cancellationToken = default);
}

public interface IMLPredictionServiceClient
{
    Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetModelStatusAsync(CancellationToken cancellationToken = default);
    Task<object?> GetAiConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Probes every upstream microservice in parallel and produces the status board.
/// </summary>
public interface IServiceStatusService
{
    Task<IReadOnlyList<ServiceStatusSnapshot>> GetBoardAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<ServiceStatusSnapshot> GetServiceAsync(MicroserviceName service, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
