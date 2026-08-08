namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record SedentaryActivityResponseDto(string UserId, int DailySteps, double ActiveMinutes, double SedentaryHours, double CaloriesBurned, List<int> HourlyHeatmap, double SedentaryScore = 0, string? RiskLevel = null, int DailyStepsTarget = 0, int ActiveMinutesTarget = 0, double StepsProgress = 0, double ActiveProgress = 0);
public record CompanyAdherenceResponseDto(string CompanyId, double AdherencePercentage, int TotalEmployees, int ActiveEmployees, List<string> HighRiskDepartments);

public interface ISedentaryEngineServiceClient
{
    Task<SedentaryActivityResponseDto?> GetUserActivityAsync(string userId, CancellationToken cancellationToken = default);
    Task<CompanyAdherenceResponseDto?> GetCompanyAdherenceAsync(string companyId, CancellationToken cancellationToken = default);
}
