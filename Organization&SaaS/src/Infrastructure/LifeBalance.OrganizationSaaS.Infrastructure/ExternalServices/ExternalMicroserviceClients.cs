using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using LifeBalance.OrganizationSaaS.Application.Interfaces;

namespace LifeBalance.OrganizationSaaS.Infrastructure.ExternalServices;

public class AuthProfileServiceClient : IAuthProfileServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthProfileServiceClient> _logger;

    public AuthProfileServiceClient(HttpClient httpClient, ILogger<AuthProfileServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ValidateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/users/{userId}/validate", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user {UserId} with AuthProfileService", userId);
            return false;
        }
    }

    public async Task<object?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<object>($"/api/v1/users/{userId}/profile", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user profile for {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateUserOrganizationAsync(string userId, string organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/users/{userId}/organization", new { OrganizationId = organizationId }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update organization for user {UserId}", userId);
            return false;
        }
    }
}

public class DashboardServiceClient : IDashboardServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardServiceClient> _logger;

    public DashboardServiceClient(HttpClient httpClient, ILogger<DashboardServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendOrganizationalMetricsAsync(string tenantId, object metricsPayload, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/api/v1/metrics/organizations/{tenantId}", metricsPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch metrics payload to DashboardService for tenant {TenantId}", tenantId);
        }
    }
}

public class ReportingServiceClient : IReportingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportingServiceClient> _logger;

    public ReportingServiceClient(HttpClient httpClient, ILogger<ReportingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task RegisterSaaSReportDataAsync(string tenantId, object reportData, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/api/v1/reports/saas/{tenantId}", reportData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report data for tenant {TenantId}", tenantId);
        }
    }
}

public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendInvitationNotificationAsync(string email, string invitationLink, string tenantName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/v1/notifications/email/invitation", new { Email = email, InvitationLink = invitationLink, TenantName = tenantName }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", email);
        }
    }

    public async Task SendLicenseExpiringNotificationAsync(string email, string licenseKey, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/v1/notifications/email/license-expiring", new { Email = email, LicenseKey = licenseKey, ExpiresAt = expiresAt }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send license expiring email to {Email}", email);
        }
    }

    public async Task SendMembershipRenewedNotificationAsync(string email, string planName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/v1/notifications/email/membership-renewed", new { Email = email, PlanName = planName }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send membership renewed email to {Email}", email);
        }
    }

    public async Task SendOrganizationSuspendedNotificationAsync(string email, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/v1/notifications/email/organization-suspended", new { Email = email, Reason = reason }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send organization suspended notification to {Email}", email);
        }
    }
}

public class GamificationServiceClient : IGamificationServiceClient
{
    private readonly HttpClient _httpClient;

    public GamificationServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<object?> GetOrganizationalChallengesAsync(string tenantId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<object>($"/api/v1/challenges/organizations/{tenantId}", cancellationToken);

    public async Task<object?> GetFamilyRankingsAsync(string familyId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<object>($"/api/v1/rankings/families/{familyId}", cancellationToken);
}

public class MLPredictionServiceClient : IMLPredictionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLPredictionServiceClient> _logger;

    public MLPredictionServiceClient(HttpClient httpClient, ILogger<MLPredictionServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendAnonymizedDataForMlAsync(string tenantId, object anonymizedMetrics, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/v1/ml/dataset", new { TenantId = tenantId, Data = anonymizedMetrics }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream anonymized ML data for tenant {TenantId}", tenantId);
        }
    }
}

public class AdministrationServiceClient : IAdministrationServiceClient
{
    private readonly HttpClient _httpClient;

    public AdministrationServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<object?> GetGlobalCatalogAsync(string catalogName, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<object>($"/api/v1/catalogs/{catalogName}", cancellationToken);
}
