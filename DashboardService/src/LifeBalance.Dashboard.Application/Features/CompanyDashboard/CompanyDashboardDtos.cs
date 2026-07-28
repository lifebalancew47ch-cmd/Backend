using LifeBalance.Dashboard.Application.Common.Interfaces;

namespace LifeBalance.Dashboard.Application.Features.CompanyDashboard;

public record CompanyDashboardResponse(
    string CompanyId,
    CompanyAdherenceResponseDto? Adherence,
    CompanyLicenseDto? Licenses,
    List<DepartmentSummaryDto> Departments
);

public record CompanyKpisResponse(string CompanyId, double AdherencePercentage, int TotalEmployees, int HighRiskCount);
public record CompanyStatisticsResponse(string CompanyId, double TotalSedentaryHours, double TotalActiveMinutes);
public record CompanyDepartmentsResponse(string CompanyId, List<DepartmentSummaryDto> Departments);
public record CompanyHeatmapResponse(string CompanyId, List<int> DepartmentHeatmap);
public record CompanyAdherenceResponse(string CompanyId, CompanyAdherenceResponseDto Adherence);
public record CompanyTrendsResponse(string CompanyId, List<double> MonthlyAdherenceTrend);
public record CompanyRankingResponse(string CompanyId, List<DepartmentRankDto> DepartmentRankings);
public record DepartmentRankDto(string DepartmentId, string DepartmentName, double Score, int Rank);
public record CompanyLicensesResponse(string CompanyId, CompanyLicenseDto Licenses);
public record CompanyOrganizationResponse(string CompanyId, int TotalDepartments, int TotalEmployees, List<string> DepartmentNames);
