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
}
