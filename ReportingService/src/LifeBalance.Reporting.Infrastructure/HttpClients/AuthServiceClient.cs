using System.Net.Http.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="IAuthServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="AuthServiceClient"/>.</summary>
    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthUserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _httpClient.GetFromJsonAsync<AuthUserProfileDto>(
                $"/api/v1/users/{userId}", cancellationToken);
            if (profile != null) return profile;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve profile for UserId: {UserId}", userId);
        }

        // DevSecOps Rule 5 (fail-closed): never simulate upstream data. Return null so
        // callers surface UpstreamServiceUnavailableException (503) instead of a fake profile.
        return null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuthUserProfileDto>?> GetFamilyMembersAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _httpClient.GetFromJsonAsync<List<AuthUserProfileDto>>(
                $"/api/v1/families/{familyId}/members", cancellationToken);
            if (members != null) return members;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family members for FamilyId: {FamilyId}", familyId);
        }

        // DevSecOps Rule 5 (fail-closed): return null (not an empty list) so upstream
        // unavailability is visible (503) instead of being silently masked as "no members".
        return null;
    }
}
