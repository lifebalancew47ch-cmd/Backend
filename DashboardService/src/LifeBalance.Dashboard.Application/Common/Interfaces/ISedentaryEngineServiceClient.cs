namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record SedentaryActivityResponseDto(string UserId, int DailySteps, double ActiveMinutes, double SedentaryHours, double CaloriesBurned, List<int> HourlyHeatmap);
public record CompanyAdherenceResponseDto(string CompanyId, double AdherencePercentage, int TotalEmployees, int ActiveEmployees, List<string> HighRiskDepartments);

public interface ISedentaryEngineServiceClient
{
    Task<SedentaryActivityResponseDto?> GetUserActivityAsync(string userId, CancellationToken cancellationToken = default);
    Task<CompanyAdherenceResponseDto?> GetCompanyAdherenceAsync(string companyId, CancellationToken cancellationToken = default);
}
