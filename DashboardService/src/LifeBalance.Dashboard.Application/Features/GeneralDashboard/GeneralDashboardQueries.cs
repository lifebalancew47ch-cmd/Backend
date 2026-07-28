using MediatR;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Shared.Results;

namespace LifeBalance.Dashboard.Application.Features.GeneralDashboard;

public record GetGeneralSummaryQuery : IRequest<Result<GeneralSummaryResponse>>;
public record GetGeneralIndicatorsQuery : IRequest<Result<GeneralIndicatorsResponse>>;
public record GetGeneralKpisQuery : IRequest<Result<GeneralKpisResponse>>;
public record GetGeneralSystemQuery : IRequest<Result<GeneralSystemResponse>>;
public record GetGeneralHealthQuery : IRequest<Result<GeneralHealthResponse>>;
public record GetGeneralVersionQuery : IRequest<Result<GeneralVersionResponse>>;

public class GeneralDashboardQueryHandlers :
    IRequestHandler<GetGeneralSummaryQuery, Result<GeneralSummaryResponse>>,
    IRequestHandler<GetGeneralIndicatorsQuery, Result<GeneralIndicatorsResponse>>,
    IRequestHandler<GetGeneralKpisQuery, Result<GeneralKpisResponse>>,
    IRequestHandler<GetGeneralSystemQuery, Result<GeneralSystemResponse>>,
    IRequestHandler<GetGeneralHealthQuery, Result<GeneralHealthResponse>>,
    IRequestHandler<GetGeneralVersionQuery, Result<GeneralVersionResponse>>
{
    private readonly IReportingServiceClient _reportingClient;

    public GeneralDashboardQueryHandlers(IReportingServiceClient reportingClient)
    {
        _reportingClient = reportingClient;
    }

    public async Task<Result<GeneralSummaryResponse>> Handle(GetGeneralSummaryQuery request, CancellationToken cancellationToken)
    {
        var metrics = await _reportingClient.GetSystemMetricsAsync(cancellationToken);
        return Result.Success(new GeneralSummaryResponse(
            metrics?.ActiveUsersToday ?? 1250,
            metrics?.PlatformHealthPercentage ?? 99.8,
            "Healthy"
        ));
    }

    public async Task<Result<GeneralIndicatorsResponse>> Handle(GetGeneralIndicatorsQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new GeneralIndicatorsResponse(8200.0, 5.8, 86.4));
    }

    public async Task<Result<GeneralKpisResponse>> Handle(GetGeneralKpisQuery request, CancellationToken cancellationToken)
    {
        var metrics = await _reportingClient.GetSystemMetricsAsync(cancellationToken);
        return Result.Success(new GeneralKpisResponse(
            metrics?.TotalUsers ?? 5000,
            450,
            35
        ));
    }

    public async Task<Result<GeneralSystemResponse>> Handle(GetGeneralSystemQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new GeneralSystemResponse(
            "Dashboard Service Aggregator",
            "Online",
            DateTime.UtcNow,
            "Production"
        ));
    }

    public async Task<Result<GeneralHealthResponse>> Handle(GetGeneralHealthQuery request, CancellationToken cancellationToken)
    {
        var healthDict = new Dictionary<string, string>
        {
            { "AuthService", "Healthy" },
            { "MedicalDataService", "Healthy" },
            { "SedentaryEngineService", "Healthy" },
            { "GamificationService", "Healthy" },
            { "NotificationService", "Healthy" },
            { "MlPredictionService", "Healthy" },
            { "OrganizationService", "Healthy" },
            { "ReportingService", "Healthy" },
            { "MongoDB", "Healthy" }
        };

        return Result.Success(new GeneralHealthResponse("Healthy", healthDict));
    }

    public async Task<Result<GeneralVersionResponse>> Handle(GetGeneralVersionQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new GeneralVersionResponse("1.0.0", "1.0.0.20260728", "git-sha-f82a9b"));
    }
}
