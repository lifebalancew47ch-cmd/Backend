namespace LifeBalance.OrganizationSaaS.Application.Interfaces;

public interface ITenantContext
{
    string TenantId { get; }
    string? OrganizationId { get; }
    string? UserId { get; }
    string CorrelationId { get; }
    bool IsAuthenticated { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

// External Microservices Clients (HttpClientFactory + Polly Resilience)
public interface IAuthProfileServiceClient
{
    Task<bool> ValidateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<object?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserOrganizationAsync(string userId, string organizationId, CancellationToken cancellationToken = default);
}

public interface IDashboardServiceClient
{
    Task SendOrganizationalMetricsAsync(string tenantId, object metricsPayload, CancellationToken cancellationToken = default);
}

public interface IReportingServiceClient
{
    Task RegisterSaaSReportDataAsync(string tenantId, object reportData, CancellationToken cancellationToken = default);
}

public interface INotificationServiceClient
{
    Task SendInvitationNotificationAsync(string email, string invitationLink, string tenantName, CancellationToken cancellationToken = default);
    Task SendLicenseExpiringNotificationAsync(string email, string licenseKey, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task SendMembershipRenewedNotificationAsync(string email, string planName, CancellationToken cancellationToken = default);
    Task SendOrganizationSuspendedNotificationAsync(string email, string reason, CancellationToken cancellationToken = default);
}

public interface IGamificationServiceClient
{
    Task<object?> GetOrganizationalChallengesAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<object?> GetFamilyRankingsAsync(string familyId, CancellationToken cancellationToken = default);
}

public interface IMLPredictionServiceClient
{
    Task SendAnonymizedDataForMlAsync(string tenantId, object anonymizedMetrics, CancellationToken cancellationToken = default);
}

public interface IAdministrationServiceClient
{
    Task<object?> GetGlobalCatalogAsync(string catalogName, CancellationToken cancellationToken = default);
}
