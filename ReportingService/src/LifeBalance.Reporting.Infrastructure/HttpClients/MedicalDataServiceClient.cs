using System.Text.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="IMedicalDataServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Handles the <c>{ success, message, data }</c> envelope used by the Medical Data service.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class MedicalDataServiceClient : IMedicalDataServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            var readings = await GetHistoryReadingsAsync(userId, from, to, cancellationToken);
            if (readings is null || readings.Count == 0)
            {
                return readings;
            }

            var biometrics = await GetLatestBiometricsAsync(userId, cancellationToken);
            if (biometrics is not null)
            {
                var latestIndex = readings.Count - 1;
                var latest = readings[latestIndex];
                readings[latestIndex] = latest with
                {
                    SystolicBp = latest.SystolicBp ?? biometrics.SystolicBp,
                    DiastolicBp = latest.DiastolicBp ?? biometrics.DiastolicBp,
                    Weight = latest.Weight ?? biometrics.Weight,
                    Height = latest.Height ?? biometrics.Height
                };
            }

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
            using var response = await _httpClient.GetAsync($"/api/v1/medical/family/{familyId}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var members = await ReadJsonWithEnvelopeAsync<List<BiometricsResponseDto>>(response, cancellationToken);
            if (members is null)
            {
                return null;
            }

            if (members.Count == 0)
            {
                return [];
            }

            return members
                .OrderByDescending(m => m.RecordedAt)
                .Select(m => new MedicalReadingDto(
                    Id: $"{m.UserId ?? familyId}:{m.RecordedAt:O}",
                    UserId: m.UserId ?? familyId,
                    FamilyId: familyId,
                    CompanyId: null,
                    HeartRate: m.HeartRate,
                    Hrv: null,
                    Spo2: null,
                    Steps: 0,
                    Latitude: null,
                    Longitude: null,
                    AccelerometerX: null,
                    AccelerometerY: null,
                    AccelerometerZ: null,
                    GyroscopeX: null,
                    GyroscopeY: null,
                    GyroscopeZ: null,
                    SystolicBp: m.SystolicBp,
                    DiastolicBp: m.DiastolicBp,
                    Weight: m.Weight,
                    Height: m.Height,
                    DeviceId: null,
                    RecordedAtUtc: m.RecordedAt ?? DateTime.UtcNow,
                    CreatedAtUtc: m.RecordedAt ?? DateTime.UtcNow))
                .ToList();
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
            using var response = await _httpClient.GetAsync(
                $"/api/v1/medical/readings/companies/{companyId}?from={from:O}&to={to:O}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<List<MedicalReadingDto>>(response, cancellationToken);
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
            using var response = await _httpClient.GetAsync($"/api/v1/medical/biometrics/{userId}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var biometrics = await ReadJsonWithEnvelopeAsync<BiometricsResponseDto>(response, cancellationToken);
            if (biometrics is null)
            {
                return null;
            }

            return new LatestBiometricsDto(
                biometrics.UserId ?? userId,
                biometrics.HeartRate ?? 0,
                biometrics.SystolicBp ?? 0,
                biometrics.DiastolicBp ?? 0,
                biometrics.Weight ?? 0,
                biometrics.Height ?? 0,
                biometrics.Bmi ?? 0,
                biometrics.RecordedAt ?? DateTime.UtcNow);
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
            using var response = await _httpClient.GetAsync("/api/v1/medical/analytics/daily-active-users", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<DailyActiveUsersDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve daily active users");
            return null;
        }
    }

    private async Task<List<MedicalReadingDto>?> GetHistoryReadingsAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"/api/v1/medical/history?from={from:O}&to={to:O}&page=1&pageSize=50", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var raw = await ReadJsonWithEnvelopeAsync<List<MedicalHistoryResponseDto>>(response, cancellationToken);
        if (raw is null)
        {
            return null;
        }

        return raw
            .OrderBy(r => r.RecordedAtUtc)
            .Select(r => new MedicalReadingDto(
                Id: r.Id ?? Guid.NewGuid().ToString(),
                UserId: r.UserId ?? userId,
                FamilyId: null,
                CompanyId: null,
                HeartRate: r.HeartRate,
                Hrv: r.Hrv,
                Spo2: r.Spo2,
                Steps: r.Steps ?? 0,
                Latitude: r.Latitude,
                Longitude: r.Longitude,
                AccelerometerX: null,
                AccelerometerY: null,
                AccelerometerZ: null,
                GyroscopeX: null,
                GyroscopeY: null,
                GyroscopeZ: null,
                SystolicBp: null,
                DiastolicBp: null,
                Weight: null,
                Height: null,
                DeviceId: null,
                RecordedAtUtc: r.RecordedAtUtc ?? DateTime.UtcNow,
                CreatedAtUtc: r.RecordedAtUtc ?? DateTime.UtcNow))
            .ToList();
    }

    private static async Task<T?> ReadJsonWithEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = jsonDoc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("data", out var dataProp) || root.TryGetProperty("Data", out dataProp))
            {
                if (dataProp.ValueKind != JsonValueKind.Null && dataProp.ValueKind != JsonValueKind.Undefined)
                {
                    return JsonSerializer.Deserialize<T>(dataProp.GetRawText(), JsonOptions);
                }
            }
        }

        return JsonSerializer.Deserialize<T>(root.GetRawText(), JsonOptions);
    }

    private sealed record MedicalHistoryResponseDto(
        string? Id,
        string? UserId,
        double? HeartRate,
        double? Hrv,
        double? Spo2,
        int? Steps,
        double? Latitude,
        double? Longitude,
        DateTime? RecordedAtUtc);

    private sealed record BiometricsResponseDto(
        string? UserId,
        double? HeartRate,
        double? SystolicBp,
        double? DiastolicBp,
        double? Weight,
        double? Height,
        double? Bmi,
        DateTime? RecordedAt);
}
