using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Global platform variables (singleton document). Holds platform-wide generic
/// values plus a dictionary of extensible global variables.
/// </summary>
public class GlobalConfiguration : AggregateRoot
{
    /// <summary>Stable id used for the singleton global configuration document.</summary>
    public const string SingletonId = "000000000000000000000002";

    public string ApplicationName { get; private set; } = "LifeBalance";
    public string FrontendBaseUrl { get; private set; } = string.Empty;
    public string SupportEmail { get; private set; } = string.Empty;
    public string DefaultLanguage { get; private set; } = "es";
    public string DefaultTimeZone { get; private set; } = "UTC";
    public int MaxUploadSizeMb { get; private set; } = 50;
    public int SessionTimeoutMinutes { get; private set; } = 60;
    public Dictionary<string, string> GlobalVariables { get; private set; } = new();

    public string UpdatedBy { get; private set; } = "system";

    private GlobalConfiguration() { }

    public static GlobalConfiguration CreateDefaults(string updatedBy = "system")
    {
        return new GlobalConfiguration
        {
            Id = SingletonId,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };
    }

    public void Apply(string applicationName,
                      string frontendBaseUrl,
                      string supportEmail,
                      string defaultLanguage,
                      string defaultTimeZone,
                      int maxUploadSizeMb,
                      int sessionTimeoutMinutes,
                      Dictionary<string, string>? globalVariables,
                      string updatedBy)
    {
        ApplicationName = string.IsNullOrWhiteSpace(applicationName) ? "LifeBalance" : applicationName;
        FrontendBaseUrl = frontendBaseUrl;
        SupportEmail = supportEmail;
        DefaultLanguage = defaultLanguage;
        DefaultTimeZone = defaultTimeZone;
        MaxUploadSizeMb = maxUploadSizeMb;
        SessionTimeoutMinutes = sessionTimeoutMinutes;
        GlobalVariables = globalVariables ?? new Dictionary<string, string>();
        UpdatedBy = updatedBy;
        Touch();
    }

    public void ResetToDefaults(string updatedBy)
    {
        Apply("LifeBalance", string.Empty, string.Empty, "es", "UTC", 50, 60, null, updatedBy);
    }
}
