using MediatR;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Exceptions;
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
        var metrics = await _reportingClient.GetSystemMetricsAsync(cancellationToken)
            ?? throw new UpstreamServiceUnavailableException("Global system metrics are unavailable from the Reporting service.");
        var status = metrics.PlatformHealthPercentage >= 90 ? "Healthy" : "Degraded";

        return Result.Success(new GeneralSummaryResponse(
            metrics.ActiveUsersToday,
            metrics.PlatformHealthPercentage,
            status
        ));
    }

    public async Task<Result<GeneralIndicatorsResponse>> Handle(GetGeneralIndicatorsQuery request, CancellationToken cancellationToken)
    {
        throw new UpstreamServiceUnavailableException(
            "General indicators are unavailable because no upstream platform indicators source is configured.");
    }

    public async Task<Result<GeneralKpisResponse>> Handle(GetGeneralKpisQuery request, CancellationToken cancellationToken)
    {
        var metrics = await _reportingClient.GetSystemMetricsAsync(cancellationToken)
            ?? throw new UpstreamServiceUnavailableException("Global system metrics are unavailable from the Reporting service.");
        return Result.Success(new GeneralKpisResponse(
            metrics.TotalUsers,
            0,
            0
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
        throw new UpstreamServiceUnavailableException(
            "Platform component health is unavailable because no health data was reported by the upstream services.");
    }

    public async Task<Result<GeneralVersionResponse>> Handle(GetGeneralVersionQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new GeneralVersionResponse("1.0.0", "1.0.0.20260728", "git-sha-f82a9b"));
    }
}
