using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Configuration section: sedentary behavior limits used by the sedentary engine.
/// </summary>
public class SedentarySettings
{
    public int MaxSedentaryMinutes { get; set; } = 90;
    public int MinActiveBreakMinutes { get; set; } = 5;

    public SedentarySettings Clone() => (SedentarySettings)MemberwiseClone();
}

/// <summary>Configuration section: device / engine synchronization intervals.</summary>
public class SyncSettings
{
    public int SyncIntervalMinutes { get; set; } = 15;

    public SyncSettings Clone() => (SyncSettings)MemberwiseClone();
}

/// <summary>Configuration section: ML prediction service behaviour.</summary>
public class AiSettings
{
    public bool Enabled { get; set; } = true;
    public string PredictionServiceUrl { get; set; } = string.Empty;
    public int ModelUpdateIntervalDays { get; set; } = 30;
    public double ConfidenceThreshold { get; set; } = 0.8;
    public int DataRetentionDays { get; set; } = 90;

    public AiSettings Clone() => (AiSettings)MemberwiseClone();
}

/// <summary>Configuration section: dashboard rendering &amp; caching.</summary>
public class DashboardSettings
{
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int CacheExpirationMinutes { get; set; } = 30;
    public int MaxWidgetsPerUser { get; set; } = 12;

    public DashboardSettings Clone() => (DashboardSettings)MemberwiseClone();
}

/// <summary>Configuration section: reporting service behaviour.</summary>
public class ReportSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxReportDays { get; set; } = 365;
    public string DefaultExportFormat { get; set; } = "PDF";

    public ReportSettings Clone() => (ReportSettings)MemberwiseClone();
}

/// <summary>Configuration section: alert engine limits.</summary>
public class AlertSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxAlertsPerDay { get; set; } = 50;

    public AlertSettings Clone() => (AlertSettings)MemberwiseClone();
}

/// <summary>Configuration section: outbound e-mail service.</summary>
public class EmailSettings
{
    public bool Enabled { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool RequireSsl { get; set; } = true;

    public EmailSettings Clone() => (EmailSettings)MemberwiseClone();
}

/// <summary>Configuration section: push notification service.</summary>
public class PushSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxPushPerDay { get; set; } = 30;
    public bool WearEnabled { get; set; } = true;

    public PushSettings Clone() => (PushSettings)MemberwiseClone();
}

/// <summary>Configuration section: notification rules.</summary>
public class NotificationSettings
{
    public bool DigestEnabled { get; set; } = true;
    public int DigestHour { get; set; } = 8;
    public int MaxNotificationsPerDay { get; set; } = 100;

    public NotificationSettings Clone() => (NotificationSettings)MemberwiseClone();
}

/// <summary>Configuration section: SaaS platform rules.</summary>
public class SaasSettings
{
    public bool AllowSelfSignup { get; set; } = true;
    public int TrialDays { get; set; } = 14;
    public int MaxOrganizationsPerAccount { get; set; } = 1;

    public SaasSettings Clone() => (SaasSettings)MemberwiseClone();
}

/// <summary>Configuration section: general platform rules.</summary>
public class SystemRulesSettings
{
    public int IdleThresholdMinutes { get; set; } = 90;
    public int MinActiveBreakMinutes { get; set; } = 5;
    public int MaxSedentaryStreakDays { get; set; } = 30;
    public bool AllowAnonymousAccess { get; set; } = false;
    public string DefaultLanguage { get; set; } = "es";
    public string DefaultTimeZone { get; set; } = "UTC";

    public SystemRulesSettings Clone() => (SystemRulesSettings)MemberwiseClone();
}
