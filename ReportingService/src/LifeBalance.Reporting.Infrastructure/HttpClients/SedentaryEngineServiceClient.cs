using System.Net.Http.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="ISedentaryEngineServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class SedentaryEngineServiceClient : ISedentaryEngineServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SedentaryEngineServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="SedentaryEngineServiceClient"/>.</summary>
    public SedentaryEngineServiceClient(HttpClient httpClient, ILogger<SedentaryEngineServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SedentaryDailyDto>?> GetUserHistoryAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<SedentaryDailyDto>>(
                $"/api/v1/sedentary/users/{userId}/history?from={from:O}&to={to:O}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary history for UserId: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GoalDto>?> GetUserGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<GoalDto>>(
                $"/api/v1/sedentary/users/{userId}/goals", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve goals for UserId: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<FamilyComplianceDto?> GetFamilyComplianceAsync(
        string familyId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<FamilyComplianceDto>(
                $"/api/v1/sedentary/families/{familyId}/compliance?from={from:O}&to={to:O}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve compliance for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CompanyAdherenceDto?> GetCompanyAdherenceAsync(
        string companyId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CompanyAdherenceDto>(
                $"/api/v1/sedentary/companies/{companyId}/adherence?from={from:O}&to={to:O}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve adherence for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }
}
