using System.Text.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="ISedentaryEngineServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Handles both raw JSON DTOs and envelope-wrapped (<c>{ success, message, data }</c>) responses from backapi services.
/// </summary>
public sealed class SedentaryEngineServiceClient : ISedentaryEngineServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SedentaryEngineServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="SedentaryEngineServiceClient"/>.</summary>
    public SedentaryEngineServiceClient(HttpClient httpClient, ILogger<SedentaryEngineServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SedentaryScoreDto?> GetUserScoreAsync(string userId, CancellationToken cancellationToken = default)
    {
        var progressTask = GetProgressAsync(userId, cancellationToken);
        var scoreTask = GetScoreAsync(userId, cancellationToken);

        var progress = await progressTask;
        var score = await scoreTask;

        if (progress is null && score is null)
        {
            return null;
        }

        return new SedentaryScoreDto(
            userId,
            progress?.DailySteps ?? 0,
            progress?.ActiveMinutes ?? 0,
            0,
            0,
            score?.Score ?? 0);
    }

    private async Task<SedentaryProgressResponseDto?> GetProgressAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/v1/sedentary/progress", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<SedentaryProgressResponseDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary progress for UserId: {UserId}", userId);
            return null;
        }
    }

    private async Task<SedentaryScoreResponseDto?> GetScoreAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/v1/sedentary/score", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<SedentaryScoreResponseDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary score for UserId: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SedentaryDailyDto>?> GetUserHistoryAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = new[]
            {
                $"/api/v1/sedentary/users/{userId}/history?from={from:O}&to={to:O}",
                $"/api/v1/sedentary/user/{userId}/history?from={from:O}&to={to:O}",
                $"/api/v1/sedentary/history?from={from:O}&to={to:O}"
            };

            foreach (var endpoint in endpoints)
            {
                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (!response.IsSuccessStatusCode) continue;

                var list = await ReadJsonWithEnvelopeAsync<List<SedentaryHistoryItemDto>>(response, cancellationToken);
                if (list is { Count: > 0 })
                {
                    return list.Select(item => new SedentaryDailyDto(
                        Date: item.Date != default ? item.Date : item.RecordedAtUtc,
                        SedentaryScore: item.SedentaryScore != 0 ? item.SedentaryScore : item.Score,
                        SedentaryHours: item.SedentaryHours,
                        ActiveMinutes: item.ActiveMinutes,
                        Steps: item.Steps != 0 ? item.Steps : item.DailySteps,
                        BreakCount: item.BreakCount
                    )).ToList();
                }
            }

            return null;
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
            var endpoints = new[]
            {
                "/api/v1/sedentary/goals",
                $"/api/v1/sedentary/users/{userId}/goals"
            };

            foreach (var endpoint in endpoints)
            {
                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    var root = jsonDoc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object &&
                        (root.TryGetProperty("data", out var dataProp) || root.TryGetProperty("Data", out dataProp)))
                    {
                        root = dataProp;
                    }

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var list = JsonSerializer.Deserialize<List<GoalResponseDto>>(root.GetRawText(), JsonOptions);
                        if (list != null)
                        {
                            return list.Select(g => new GoalDto(
                                g.Id ?? Guid.NewGuid().ToString(),
                                "Daily Steps Target",
                                "Steps",
                                g.DailyStepsTarget ?? 8000,
                                0,
                                false,
                                null)).ToList();
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        var goal = JsonSerializer.Deserialize<GoalResponseDto>(root.GetRawText(), JsonOptions);
                        if (goal != null)
                        {
                            return [new GoalDto(
                                goal.Id ?? Guid.NewGuid().ToString(),
                                "Daily Steps Target",
                                "Steps",
                                goal.DailyStepsTarget ?? 8000,
                                0,
                                false,
                                null)];
                        }
                    }
                }
            }

            return [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve goals for UserId: {UserId}", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<FamilyComplianceDto?> GetFamilyComplianceAsync(
        string familyId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"/api/v1/sedentary/families/{familyId}/compliance?from={from:O}&to={to:O}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<FamilyComplianceDto>(response, cancellationToken);
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
            using var response = await _httpClient.GetAsync(
                $"/api/v1/sedentary/companies/{companyId}/adherence?from={from:O}&to={to:O}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            return await ReadJsonWithEnvelopeAsync<CompanyAdherenceDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve adherence for CompanyId: {CompanyId}", companyId);
            return null;
        }
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

    private sealed record SedentaryScoreResponseDto(
        string? UserId,
        double? Score,
        string? RiskLevel,
        DateTime? RecordedAtUtc);

    private sealed record SedentaryProgressResponseDto(
        double? DailySteps,
        double? DailyStepsTarget,
        double? ActiveMinutes,
        double? ActiveMinutesTarget,
        double? StepsProgress,
        double? ActiveProgress);

    private sealed record GoalResponseDto(
        string? Id, string? UserId, double? DailyStepsTarget, double? ActiveMinutesTarget, DateTime? UpdatedAtUtc);

    private sealed record SedentaryHistoryItemDto(
        DateTime Date,
        DateTime RecordedAtUtc,
        double SedentaryScore,
        double Score,
        double SedentaryHours,
        double ActiveMinutes,
        int Steps,
        int DailySteps,
        int BreakCount);
}

