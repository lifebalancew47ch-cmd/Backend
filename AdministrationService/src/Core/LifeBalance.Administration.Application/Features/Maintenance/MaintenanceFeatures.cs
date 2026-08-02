using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Maintenance;

public record MaintenanceModeDto(
    bool IsEnabled,
    string Message,
    DateTime? ScheduledEndAt,
    string? EnabledBy,
    DateTime? EnabledAt,
    string? DisabledBy,
    DateTime? DisabledAt);

// ── Commands / Queries ────────────────────────────────────────────────────
public record GetMaintenanceStatusQuery : IRequest<ApiResponse<MaintenanceModeDto>>;

public record SetMaintenanceModeCommand(
    bool IsEnabled,
    string Message = "",
    DateTime? ScheduledEndAt = null,
    string? ByUserId = null) : IRequest<ApiResponse<MaintenanceModeDto>>;

// ── Validators ────────────────────────────────────────────────────────────
public class SetMaintenanceModeCommandValidator : AbstractValidator<SetMaintenanceModeCommand>
{
    public SetMaintenanceModeCommandValidator()
    {
        When(x => x.IsEnabled, () =>
        {
            RuleFor(x => x.Message).MaximumLength(500);
            RuleFor(x => x.ScheduledEndAt).GreaterThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.ScheduledEndAt.HasValue)
                .WithMessage("ScheduledEndAt must be in the future.");
        });
    }
}

// ── Command Handler ───────────────────────────────────────────────────────
public class MaintenanceCommandHandler : IRequestHandler<SetMaintenanceModeCommand, ApiResponse<MaintenanceModeDto>>
{
    private readonly IRepository<MaintenanceMode> _maintenanceRepository;

    public MaintenanceCommandHandler(IRepository<MaintenanceMode> maintenanceRepository)
    {
        _maintenanceRepository = maintenanceRepository;
    }

    public async Task<ApiResponse<MaintenanceModeDto>> Handle(SetMaintenanceModeCommand request, CancellationToken cancellationToken)
    {
        var mode = await _maintenanceRepository.GetByIdAsync(MaintenanceMode.SingletonId, cancellationToken);
        if (mode == null)
        {
            mode = MaintenanceMode.CreateDefault();
            await _maintenanceRepository.AddAsync(mode, cancellationToken);
        }

        var actor = request.ByUserId ?? "system";
        if (request.IsEnabled)
        {
            mode.Enable(request.Message, actor, request.ScheduledEndAt);
        }
        else
        {
            mode.Disable(actor);
        }

        await _maintenanceRepository.UpdateAsync(mode, cancellationToken);
        return ApiResponse<MaintenanceModeDto>.Ok(ToDto(mode), request.IsEnabled ? "Maintenance mode enabled." : "Maintenance mode disabled.");
    }

    internal static MaintenanceModeDto ToDto(MaintenanceMode mode)
        => new(mode.IsEnabled, mode.Message, mode.ScheduledEndAt, mode.EnabledBy,
               mode.EnabledAt, mode.DisabledBy, mode.DisabledAt);
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class MaintenanceQueryHandler : IRequestHandler<GetMaintenanceStatusQuery, ApiResponse<MaintenanceModeDto>>
{
    private readonly IRepository<MaintenanceMode> _maintenanceRepository;

    public MaintenanceQueryHandler(IRepository<MaintenanceMode> maintenanceRepository)
    {
        _maintenanceRepository = maintenanceRepository;
    }

    public async Task<ApiResponse<MaintenanceModeDto>> Handle(GetMaintenanceStatusQuery request, CancellationToken cancellationToken)
    {
        var mode = await _maintenanceRepository.GetByIdAsync(MaintenanceMode.SingletonId, cancellationToken);
        if (mode == null)
        {
            mode = MaintenanceMode.CreateDefault();
            await _maintenanceRepository.AddAsync(mode, cancellationToken);
        }

        return ApiResponse<MaintenanceModeDto>.Ok(MaintenanceCommandHandler.ToDto(mode));
    }
}
