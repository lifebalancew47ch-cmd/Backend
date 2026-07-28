namespace LifeBalance.Dashboard.Application.Features.GeneralDashboard;

public record GeneralSummaryResponse(int ActiveUsers, double GlobalHealthScore, string SystemStatus);
public record GeneralIndicatorsResponse(double AverageDailySteps, double AverageSedentaryTime, double PlatformAdherenceRate);
public record GeneralKpisResponse(int TotalRegisteredUsers, int ActiveFamilies, int ActiveCompanies);
public record GeneralSystemResponse(string ServiceName, string Status, DateTime ServerTimeUtc, string Environment);
public record GeneralHealthResponse(string OverallStatus, Dictionary<string, string> ComponentHealth);
public record GeneralVersionResponse(string Version, string BuildNumber, string CommitHash);
