using MediatR;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Application.Features.Services;

public record ServiceStatusDto(
    MicroserviceName Service,
    string ServiceName,
    ServiceHealthStatus Status,
    int? StatusCode,
    string Message,
    long LatencyMs,
    string? Version,
    object? Payload,
    DateTime LastCheckedAt,
    DateTime? LastSuccessAt);

public record ServicesBoardDto(
    int Total,
    int Healthy,
    int Degraded,
    int Unhealthy,
    int Unknown,
    DateTime LastCheckedAt,
    IReadOnlyList<ServiceStatusDto> Services);

// ── Queries ───────────────────────────────────────────────────────────────
public record GetServicesStatusQuery(bool ForceRefresh = false) : IRequest<ApiResponse<ServicesBoardDto>>;

public record GetServiceStatusQuery(MicroserviceName Service, bool ForceRefresh = false) : IRequest<ApiResponse<ServiceStatusDto>>;

// ── Query Handler ─────────────────────────────────────────────────────────
public class ServicesQueryHandler :
    IRequestHandler<GetServicesStatusQuery, ApiResponse<ServicesBoardDto>>,
    IRequestHandler<GetServiceStatusQuery, ApiResponse<ServiceStatusDto>>
{
    private readonly IServiceStatusService _statusService;

    public ServicesQueryHandler(IServiceStatusService statusService)
    {
        _statusService = statusService;
    }

    public async Task<ApiResponse<ServicesBoardDto>> Handle(GetServicesStatusQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _statusService.GetBoardAsync(request.ForceRefresh, cancellationToken);

        var board = new ServicesBoardDto(
            snapshots.Count,
            snapshots.Count(s => s.Status == ServiceHealthStatus.Healthy),
            snapshots.Count(s => s.Status == ServiceHealthStatus.Degraded),
            snapshots.Count(s => s.Status == ServiceHealthStatus.Unhealthy),
            snapshots.Count(s => s.Status == ServiceHealthStatus.Unknown),
            snapshots.Count == 0 ? DateTime.UtcNow : snapshots.Max(s => s.LastCheckedAt),
            snapshots.Select(ToDto).ToList());

        return ApiResponse<ServicesBoardDto>.Ok(board, "Services status retrieved.");
    }

    public async Task<ApiResponse<ServiceStatusDto>> Handle(GetServiceStatusQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _statusService.GetServiceAsync(request.Service, request.ForceRefresh, cancellationToken);
        return ApiResponse<ServiceStatusDto>.Ok(ToDto(snapshot));
    }

    private static ServiceStatusDto ToDto(ServiceStatusSnapshot snapshot)
        => new(
            snapshot.Service,
            snapshot.ServiceName,
            snapshot.Status,
            snapshot.StatusCode,
            snapshot.Message,
            snapshot.LatencyMs,
            snapshot.Version,
            snapshot.Payload,
            snapshot.LastCheckedAt,
            snapshot.LastSuccessAt);
}
