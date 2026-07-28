using MediatR;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Shared.Results;

namespace LifeBalance.Dashboard.Application.Features.CompanyDashboard;

public record GetCompanyDashboardQuery(string CompanyId) : IRequest<Result<CompanyDashboardResponse>>;
public record GetCompanyKpisQuery(string CompanyId) : IRequest<Result<CompanyKpisResponse>>;
public record GetCompanyStatisticsQuery(string CompanyId) : IRequest<Result<CompanyStatisticsResponse>>;
public record GetCompanyDepartmentsQuery(string CompanyId) : IRequest<Result<CompanyDepartmentsResponse>>;
public record GetCompanyHeatmapQuery(string CompanyId) : IRequest<Result<CompanyHeatmapResponse>>;
public record GetCompanyAdherenceQuery(string CompanyId) : IRequest<Result<CompanyAdherenceResponse>>;
public record GetCompanyTrendsQuery(string CompanyId) : IRequest<Result<CompanyTrendsResponse>>;
public record GetCompanyRankingQuery(string CompanyId) : IRequest<Result<CompanyRankingResponse>>;
public record GetCompanyLicensesQuery(string CompanyId) : IRequest<Result<CompanyLicensesResponse>>;
public record GetCompanyOrganizationQuery(string CompanyId) : IRequest<Result<CompanyOrganizationResponse>>;

public class CompanyDashboardQueryHandlers :
    IRequestHandler<GetCompanyDashboardQuery, Result<CompanyDashboardResponse>>,
    IRequestHandler<GetCompanyKpisQuery, Result<CompanyKpisResponse>>,
    IRequestHandler<GetCompanyStatisticsQuery, Result<CompanyStatisticsResponse>>,
    IRequestHandler<GetCompanyDepartmentsQuery, Result<CompanyDepartmentsResponse>>,
    IRequestHandler<GetCompanyHeatmapQuery, Result<CompanyHeatmapResponse>>,
    IRequestHandler<GetCompanyAdherenceQuery, Result<CompanyAdherenceResponse>>,
    IRequestHandler<GetCompanyTrendsQuery, Result<CompanyTrendsResponse>>,
    IRequestHandler<GetCompanyRankingQuery, Result<CompanyRankingResponse>>,
    IRequestHandler<GetCompanyLicensesQuery, Result<CompanyLicensesResponse>>,
    IRequestHandler<GetCompanyOrganizationQuery, Result<CompanyOrganizationResponse>>
{
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IOrganizationServiceClient _orgClient;

    public CompanyDashboardQueryHandlers(
        ISedentaryEngineServiceClient sedentaryClient,
        IOrganizationServiceClient orgClient)
    {
        _sedentaryClient = sedentaryClient;
        _orgClient = orgClient;
    }

    public async Task<Result<CompanyDashboardResponse>> Handle(GetCompanyDashboardQuery request, CancellationToken cancellationToken)
    {
        var adherenceTask = _sedentaryClient.GetCompanyAdherenceAsync(request.CompanyId, cancellationToken);
        var licensesTask = _orgClient.GetCompanyLicensesAsync(request.CompanyId, cancellationToken);
        var departmentsTask = _orgClient.GetDepartmentsAsync(request.CompanyId, cancellationToken);

        await Task.WhenAll(adherenceTask, licensesTask, departmentsTask);

        var adherence = await adherenceTask;
        var licenses = await licensesTask;
        var departments = await departmentsTask ?? new List<DepartmentSummaryDto>();

        return Result.Success(new CompanyDashboardResponse(request.CompanyId, adherence, licenses, departments));
    }

    public async Task<Result<CompanyKpisResponse>> Handle(GetCompanyKpisQuery request, CancellationToken cancellationToken)
    {
        var adherence = await _sedentaryClient.GetCompanyAdherenceAsync(request.CompanyId, cancellationToken);
        return Result.Success(new CompanyKpisResponse(
            request.CompanyId,
            adherence?.AdherencePercentage ?? 82.5,
            adherence?.TotalEmployees ?? 150,
            adherence?.HighRiskDepartments.Count ?? 2
        ));
    }

    public async Task<Result<CompanyStatisticsResponse>> Handle(GetCompanyStatisticsQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new CompanyStatisticsResponse(request.CompanyId, 1250.0, 3400.0));
    }

    public async Task<Result<CompanyDepartmentsResponse>> Handle(GetCompanyDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var depts = await _orgClient.GetDepartmentsAsync(request.CompanyId, cancellationToken);
        return Result.Success(new CompanyDepartmentsResponse(request.CompanyId, depts ?? new List<DepartmentSummaryDto>()));
    }

    public async Task<Result<CompanyHeatmapResponse>> Handle(GetCompanyHeatmapQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new CompanyHeatmapResponse(request.CompanyId, Enumerable.Repeat(25, 24).ToList()));
    }

    public async Task<Result<CompanyAdherenceResponse>> Handle(GetCompanyAdherenceQuery request, CancellationToken cancellationToken)
    {
        var adherence = await _sedentaryClient.GetCompanyAdherenceAsync(request.CompanyId, cancellationToken)
                        ?? new CompanyAdherenceResponseDto(request.CompanyId, 85.0, 100, 85, new List<string> { "Sales" });
        return Result.Success(new CompanyAdherenceResponse(request.CompanyId, adherence));
    }

    public async Task<Result<CompanyTrendsResponse>> Handle(GetCompanyTrendsQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new CompanyTrendsResponse(request.CompanyId, new List<double> { 70.0, 75.0, 80.0, 85.0, 88.0 }));
    }

    public async Task<Result<CompanyRankingResponse>> Handle(GetCompanyRankingQuery request, CancellationToken cancellationToken)
    {
        var depts = await _orgClient.GetDepartmentsAsync(request.CompanyId, cancellationToken) ?? new List<DepartmentSummaryDto>();
        var ranks = depts.Select((d, i) => new DepartmentRankDto(d.DepartmentId, d.Name, d.ActiveAdherenceScore, i + 1)).ToList();
        return Result.Success(new CompanyRankingResponse(request.CompanyId, ranks));
    }

    public async Task<Result<CompanyLicensesResponse>> Handle(GetCompanyLicensesQuery request, CancellationToken cancellationToken)
    {
        var lic = await _orgClient.GetCompanyLicensesAsync(request.CompanyId, cancellationToken)
                  ?? new CompanyLicenseDto(request.CompanyId, 200, 150, DateTime.UtcNow.AddYears(1), "Enterprise");
        return Result.Success(new CompanyLicensesResponse(request.CompanyId, lic));
    }

    public async Task<Result<CompanyOrganizationResponse>> Handle(GetCompanyOrganizationQuery request, CancellationToken cancellationToken)
    {
        var depts = await _orgClient.GetDepartmentsAsync(request.CompanyId, cancellationToken) ?? new List<DepartmentSummaryDto>();
        return Result.Success(new CompanyOrganizationResponse(request.CompanyId, depts.Count, depts.Sum(d => d.TotalMembers), depts.Select(d => d.Name).ToList()));
    }
}
