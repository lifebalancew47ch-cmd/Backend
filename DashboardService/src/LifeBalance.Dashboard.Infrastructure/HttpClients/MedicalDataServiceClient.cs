using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class MedicalDataServiceClient : IMedicalDataServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MedicalDataServiceClient> _logger;

    public MedicalDataServiceClient(HttpClient httpClient, ILogger<MedicalDataServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MedicalDataResponseDto?> GetUserBiometricsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<MedicalDataResponseDto>($"/api/v1/medical/biometrics/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve biometrics for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<MedicalDataResponseDto>?> GetFamilyBiometricsAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<List<MedicalDataResponseDto>>($"/api/v1/medical/family/{familyId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family biometrics for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }
}
