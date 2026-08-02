using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Global system configuration for the whole LifeBalance platform. Stored as a
/// single document (singleton) so that exactly one active configuration exists.
/// </summary>
public class SystemConfiguration : AggregateRoot
{
    /// <summary>Stable id used for the singleton configuration document.</summary>
    public const string SingletonId = "000000000000000000000001";

    public SedentarySettings Sedentary { get; private set; } = new();
    public SyncSettings Sync { get; private set; } = new();
    public AiSettings Ai { get; private set; } = new();
    public DashboardSettings Dashboard { get; private set; } = new();
    public ReportSettings Reports { get; private set; } = new();
    public AlertSettings Alerts { get; private set; } = new();
    public EmailSettings Email { get; private set; } = new();
    public PushSettings Push { get; private set; } = new();
    public NotificationSettings Notifications { get; private set; } = new();
    public SaasSettings Saas { get; private set; } = new();
    public SystemRulesSettings Rules { get; private set; } = new();

    public string UpdatedBy { get; private set; } = "system";

    private SystemConfiguration() { }

    public static SystemConfiguration CreateDefaults(string updatedBy = "system")
    {
        return new SystemConfiguration
        {
            Id = SingletonId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };
    }

    /// <summary>
    /// Applies a full replacement of the configuration values coming from a
    /// validated application-layer DTO. Anti-mass-assignment: only the allowed
    /// sections are overwritten.
    /// </summary>
    public void Apply(SedentarySettings? sedentary,
                      SyncSettings? sync,
                      AiSettings? ai,
                      DashboardSettings? dashboard,
                      ReportSettings? reports,
                      AlertSettings? alerts,
                      EmailSettings? email,
                      PushSettings? push,
                      NotificationSettings? notifications,
                      SaasSettings? saas,
                      SystemRulesSettings? rules,
                      string updatedBy)
    {
        Sedentary = sedentary ?? new SedentarySettings();
        Sync = sync ?? new SyncSettings();
        Ai = ai ?? new AiSettings();
        Dashboard = dashboard ?? new DashboardSettings();
        Reports = reports ?? new ReportSettings();
        Alerts = alerts ?? new AlertSettings();
        Email = email ?? new EmailSettings();
        Push = push ?? new PushSettings();
        Notifications = notifications ?? new NotificationSettings();
        Saas = saas ?? new SaasSettings();
        Rules = rules ?? new SystemRulesSettings();
        UpdatedBy = updatedBy;
        Touch();
    }

    public void ResetToDefaults(string updatedBy)
    {
        Apply(new SedentarySettings(), new SyncSettings(), new AiSettings(),
              new DashboardSettings(), new ReportSettings(), new AlertSettings(),
              new EmailSettings(), new PushSettings(), new NotificationSettings(),
              new SaasSettings(), new SystemRulesSettings(), updatedBy);
    }
}
