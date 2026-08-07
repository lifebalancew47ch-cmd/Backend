using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class SedentaryEngineServiceClient : ISedentaryEngineServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SedentaryEngineServiceClient> _logger;

    public SedentaryEngineServiceClient(HttpClient httpClient, ILogger<SedentaryEngineServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SedentaryActivityResponseDto?> GetUserActivityAsync(string userId, CancellationToken cancellationToken = default)
    {
        var progressTask = _httpClient.GetWrappedAsync<SedentaryProgressDto>("/api/v1/sedentary/progress", cancellationToken);
        var scoreTask = _httpClient.GetWrappedAsync<SedentaryScoreDto>("/api/v1/sedentary/score", cancellationToken);

        SedentaryProgressDto? progress;
        SedentaryScoreDto? score;
        try
        {
            progress = await progressTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary progress for UserId: {UserId}", userId);
            progress = null;
        }

        try
        {
            score = await scoreTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary score for UserId: {UserId}", userId);
            score = null;
        }

        // 1. Primary check: If progress returned non-zero step or active minute data, use it.
        if (progress != null && (progress.DailySteps > 0 || progress.ActiveMinutes > 0))
        {
            return new SedentaryActivityResponseDto(
                userId,
                progress.DailySteps,
                progress.ActiveMinutes,
                0,
                0,
                Enumerable.Repeat(0, 24).ToList());
        }

        // 2. Fallback check: If progress is null or returning zeroes for today, query history endpoints.
        var history = await GetHistoryFallbackAsync(userId, cancellationToken);
        if (history is { Count: > 0 })
        {
            var latest = history.OrderByDescending(h => h.RecordedAtUtc ?? h.Date ?? DateTime.MinValue).First();
            var steps = latest.DailySteps ?? latest.Steps ?? progress?.DailySteps ?? 0;
            var activeMinutes = latest.ActiveMinutes ?? progress?.ActiveMinutes ?? 0;
            var sedentaryHours = latest.SedentaryHours ?? 0;
            var caloriesBurned = latest.CaloriesBurned ?? 0;
            var heatmap = latest.HourlyHeatmap is { Count: 24 } ? latest.HourlyHeatmap : Enumerable.Repeat(0, 24).ToList();

            return new SedentaryActivityResponseDto(
                userId,
                steps,
                activeMinutes,
                sedentaryHours,
                caloriesBurned,
                heatmap);
        }

        if (progress is null && score is null)
        {
            return null;
        }

        return new SedentaryActivityResponseDto(
            userId,
            progress?.DailySteps ?? 0,
            progress?.ActiveMinutes ?? 0,
            0,
            0,
            Enumerable.Repeat(0, 24).ToList());
    }

    public async Task<CompanyAdherenceResponseDto?> GetCompanyAdherenceAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<CompanyAdherenceResponseDto>($"/api/v1/sedentary/company/{companyId}/adherence", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company adherence for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    private async Task<List<SedentaryHistoryItemDto>?> GetHistoryFallbackAsync(string userId, CancellationToken cancellationToken)
    {
        var endpoints = new[]
        {
            $"/api/v1/sedentary/user/{userId}/history",
            $"/api/v1/sedentary/users/{userId}/history",
            "/api/v1/sedentary/history"
        };

        foreach (var endpoint in endpoints)
        {
            try
            {
                var history = await _httpClient.GetWrappedAsync<List<SedentaryHistoryItemDto>>(endpoint, cancellationToken);
                if (history is { Count: > 0 })
                {
                    return history;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve sedentary history from {Endpoint} via GetWrappedAsync for UserId: {UserId}", endpoint, userId);
            }

            try
            {
                var history = await _httpClient.GetFromJsonAsync<List<SedentaryHistoryItemDto>>(endpoint, cancellationToken);
                if (history is { Count: > 0 })
                {
                    return history;
                }
            }
            catch
            {
                // Ignore raw endpoint fallback exception
            }
        }

        return null;
    }

    private sealed class SedentaryProgressDto
    {
        public int DailySteps { get; set; }
        public int DailyStepsTarget { get; set; }
        public double ActiveMinutes { get; set; }
        public int ActiveMinutesTarget { get; set; }
        public double StepsProgress { get; set; }
        public double ActiveProgress { get; set; }
    }

    private sealed class SedentaryScoreDto
    {
        public string? UserId { get; set; }
        public double Score { get; set; }
        public string? RiskLevel { get; set; }
        public DateTime? RecordedAtUtc { get; set; }
    }

    private sealed class SedentaryHistoryItemDto
    {
        public DateTime? Date { get; set; }
        public DateTime? RecordedAtUtc { get; set; }
        public double? SedentaryScore { get; set; }
        public double? Score { get; set; }
        public double? SedentaryHours { get; set; }
        public double? ActiveMinutes { get; set; }
        public int? Steps { get; set; }
        public int? DailySteps { get; set; }
        public int? BreakCount { get; set; }
        public double? CaloriesBurned { get; set; }
        public List<int>? HourlyHeatmap { get; set; }
    }
}
