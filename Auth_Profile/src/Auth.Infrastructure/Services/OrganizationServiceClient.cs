using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;

public class OrganizationServiceClient : IOrganizationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;
    private readonly string _provisioningKey;

    public OrganizationServiceClient(
        HttpClient httpClient,
        ILogger<OrganizationServiceClient> logger,
        IOptions<InternalSettings> internalSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _provisioningKey = internalSettings.Value.ProvisioningKey;
    }

    public async Task<TenantContextResult?> GetTenantContextAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Organization tenant resolution returned status {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<OrganizationApiResponse>(json, JsonOptions);
            if (wrapper?.Data is null)
                return null;

            return new TenantContextResult(wrapper.Data.TenantId, wrapper.Data.OrganizationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve tenant context from Organization service.");
            return null;
        }
    }

    public async Task<TenantContextResult?> ProvisionMembershipAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal/memberships")
            {
                Content = JsonContent.Create(new { userId })
            };
            request.Headers.TryAddWithoutValidation("X-Internal-Key", _provisioningKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Organization membership provisioning returned status {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<OrganizationApiResponse>(json, JsonOptions);
            if (wrapper?.Data is null)
                return null;

            return new TenantContextResult(wrapper.Data.TenantId, wrapper.Data.OrganizationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to provision membership from Organization service.");
            return null;
        }
    }

    private sealed class OrganizationApiResponse
    {
        public bool Success { get; set; }
        public TenantContextData? Data { get; set; }
    }

    private sealed class TenantContextData
    {
        public string? TenantId { get; set; }
        public string? OrganizationId { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
