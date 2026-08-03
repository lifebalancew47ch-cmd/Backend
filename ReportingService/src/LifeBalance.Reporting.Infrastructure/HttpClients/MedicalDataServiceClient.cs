using System.Net.Http.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="IMedicalDataServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class MedicalDataServiceClient : IMedicalDataServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MedicalDataServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="MedicalDataServiceClient"/>.</summary>
    public MedicalDataServiceClient(HttpClient httpClient, ILogger<MedicalDataServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MedicalReadingDto>?> GetUserReadingsAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var readings = await _httpClient.GetFromJsonAsync<List<MedicalReadingDto>>(
                $"/api/v1/medical/readings/users/{userId}?from={from:O}&to={to:O}", cancellationToken);
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve readings for UserId: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MedicalReadingDto>?> GetFamilyReadingsAsync(
        string familyId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var readings = await _httpClient.GetFromJsonAsync<List<MedicalReadingDto>>(
                $"/api/v1/medical/readings/families/{familyId}?from={from:O}&to={to:O}", cancellationToken);
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve readings for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MedicalReadingDto>?> GetCompanyReadingsAsync(
        string companyId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var readings = await _httpClient.GetFromJsonAsync<List<MedicalReadingDto>>(
                $"/api/v1/medical/readings/companies/{companyId}?from={from:O}&to={to:O}", cancellationToken);
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve readings for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<LatestBiometricsDto?> GetLatestBiometricsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LatestBiometricsDto>(
                $"/api/v1/medical/biometrics/latest/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve latest biometrics for UserId: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<DailyActiveUsersDto?> GetDailyActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DailyActiveUsersDto>(
                "/api/v1/medical/analytics/daily-active-users", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve daily active users");
            return null;
        }
    }
}
