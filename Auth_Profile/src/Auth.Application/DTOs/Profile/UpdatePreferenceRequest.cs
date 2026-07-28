namespace Auth.Application.DTOs.Profile;

public record UpdatePreferenceRequest(
    string? Theme,
    string? Language,
    string? Timezone,
    string? UnitsSystem,
    bool? NotificationsEnabled,
    bool? EmailNotificationsEnabled,
    bool? PushNotificationsEnabled,
    string? ProfileVisibility,
    bool? MarketingConsent,
    bool? ActivitySharing);
