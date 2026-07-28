namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record GeneralSystemMetricsDto(int TotalUsers, int ActiveUsersToday, double PlatformHealthPercentage, string SystemVersion);

public interface IReportingServiceClient
{
    Task<GeneralSystemMetricsDto?> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
}
