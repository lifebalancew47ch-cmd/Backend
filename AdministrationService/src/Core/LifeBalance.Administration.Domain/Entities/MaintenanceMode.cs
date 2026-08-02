using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Platform maintenance mode (singleton). When enabled, only the health endpoint
/// and administrator diagnostics remain reachable.
/// </summary>
public class MaintenanceMode : AggregateRoot
{
    /// <summary>Stable id used for the singleton maintenance document.</summary>
    public const string SingletonId = "000000000000000000000003";

    public bool IsEnabled { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime? ScheduledEndAt { get; private set; }
    public string? EnabledBy { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public string? DisabledBy { get; private set; }
    public DateTime? DisabledAt { get; private set; }

    private MaintenanceMode() { }

    public static MaintenanceMode CreateDefault()
    {
        return new MaintenanceMode
        {
            Id = SingletonId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Enable(string message, string enabledBy, DateTime? scheduledEndAt = null)
    {
        IsEnabled = true;
        Message = message;
        ScheduledEndAt = scheduledEndAt;
        EnabledBy = enabledBy;
        EnabledAt = DateTime.UtcNow;
        DisabledBy = null;
        DisabledAt = null;
        Touch();
    }

    public void Disable(string disabledBy)
    {
        IsEnabled = false;
        DisabledBy = disabledBy;
        DisabledAt = DateTime.UtcNow;
        Touch();
    }
}
