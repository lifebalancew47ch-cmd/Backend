using MediatR;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Statistics;

public record AdministrativeStatisticsDto(
    long TotalCatalogs,
    long ActiveCatalogs,
    long TotalParameters,
    long ActiveParameters,
    long TotalFeatureFlags,
    long EnabledFeatureFlags,
    long TotalAuditEntries,
    long TotalLogs,
    long TotalServices,
    long HealthyServices,
    long UnhealthyServices,
    bool MaintenanceEnabled,
    DateTime? MaintenanceEnabledAt);

public record GetAdministrativeStatisticsQuery : IRequest<ApiResponse<AdministrativeStatisticsDto>>;

public class AdministrativeStatisticsQueryHandler : IRequestHandler<GetAdministrativeStatisticsQuery, ApiResponse<AdministrativeStatisticsDto>>
{
    private readonly IRepository<Catalog> _catalogRepository;
    private readonly IRepository<SystemParameter> _parameterRepository;
    private readonly IRepository<FeatureFlag> _flagRepository;
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly IRepository<SystemLog> _logRepository;
    private readonly IRepository<MaintenanceMode> _maintenanceRepository;
    private readonly IServiceStatusService _statusService;

    public AdministrativeStatisticsQueryHandler(
        IRepository<Catalog> catalogRepository,
        IRepository<SystemParameter> parameterRepository,
        IRepository<FeatureFlag> flagRepository,
        IRepository<AuditLog> auditRepository,
        IRepository<SystemLog> logRepository,
        IRepository<MaintenanceMode> maintenanceRepository,
        IServiceStatusService statusService)
    {
        _catalogRepository = catalogRepository;
        _parameterRepository = parameterRepository;
        _flagRepository = flagRepository;
        _auditRepository = auditRepository;
        _logRepository = logRepository;
        _maintenanceRepository = maintenanceRepository;
        _statusService = statusService;
    }

    public async Task<ApiResponse<AdministrativeStatisticsDto>> Handle(GetAdministrativeStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Counters run in parallel.
        var totalCatalogsTask = _catalogRepository.CountAsync(_ => true, cancellationToken);
        var activeCatalogsTask = _catalogRepository.CountAsync(x => x.IsActive, cancellationToken);
        var totalParametersTask = _parameterRepository.CountAsync(_ => true, cancellationToken);
        var activeParametersTask = _parameterRepository.CountAsync(x => x.IsActive, cancellationToken);
        var totalFlagsTask = _flagRepository.CountAsync(_ => true, cancellationToken);
        var enabledFlagsTask = _flagRepository.CountAsync(x => x.IsEnabled, cancellationToken);
        var auditCountTask = _auditRepository.CountAsync(_ => true, cancellationToken);
        var logCountTask = _logRepository.CountAsync(_ => true, cancellationToken);
        var maintenanceTask = _maintenanceRepository.GetByIdAsync(MaintenanceMode.SingletonId, cancellationToken);

        await Task.WhenAll(totalCatalogsTask, activeCatalogsTask, totalParametersTask, activeParametersTask,
            totalFlagsTask, enabledFlagsTask, auditCountTask, logCountTask, maintenanceTask);

        var board = await _statusService.GetBoardAsync(forceRefresh: false, cancellationToken);

        var stats = new AdministrativeStatisticsDto(
            await totalCatalogsTask,
            await activeCatalogsTask,
            await totalParametersTask,
            await activeParametersTask,
            await totalFlagsTask,
            await enabledFlagsTask,
            await auditCountTask,
            await logCountTask,
            board.Count,
            board.Count(s => s.Status == ServiceHealthStatus.Healthy),
            board.Count(s => s.Status == ServiceHealthStatus.Unhealthy),
            maintenanceTask.Result?.IsEnabled ?? false,
            maintenanceTask.Result?.EnabledAt);

        return ApiResponse<AdministrativeStatisticsDto>.Ok(stats);
    }
}
