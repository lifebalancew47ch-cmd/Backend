using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Settings;

// ── DTOs ──────────────────────────────────────────────────────────────────
public record SedentarySettingsDto(int MaxSedentaryMinutes, int MinActiveBreakMinutes);
public record SyncSettingsDto(int SyncIntervalMinutes);
public record AiSettingsDto(bool Enabled, string PredictionServiceUrl, int ModelUpdateIntervalDays, double ConfidenceThreshold, int DataRetentionDays);
public record DashboardSettingsDto(int RefreshIntervalSeconds, int CacheExpirationMinutes, int MaxWidgetsPerUser);
public record ReportsSettingsDto(bool Enabled, int MaxReportDays, string DefaultExportFormat);
public record AlertsSettingsDto(bool Enabled, int MaxAlertsPerDay);
public record EmailSettingsDto(bool Enabled, string FromEmail, string SmtpHost, int SmtpPort, bool RequireSsl);
public record PushSettingsDto(bool Enabled, int MaxPushPerDay, bool WearEnabled);
public record NotificationSettingsDto(bool DigestEnabled, int DigestHour, int MaxNotificationsPerDay);
public record SaasSettingsDto(bool AllowSelfSignup, int TrialDays, int MaxOrganizationsPerAccount);
public record SystemRulesSettingsDto(int IdleThresholdMinutes, int MinActiveBreakMinutes, int MaxSedentaryStreakDays, bool AllowAnonymousAccess, string DefaultLanguage, string DefaultTimeZone);

public record SystemSettingsDto(
    SedentarySettingsDto? Sedentary,
    SyncSettingsDto? Sync,
    AiSettingsDto? Ai,
    DashboardSettingsDto? Dashboard,
    ReportsSettingsDto? Reports,
    AlertsSettingsDto? Alerts,
    EmailSettingsDto? Email,
    PushSettingsDto? Push,
    NotificationSettingsDto? Notifications,
    SaasSettingsDto? Saas,
    SystemRulesSettingsDto? Rules);

public record GlobalSettingsDto(
    string? ApplicationName,
    string? FrontendBaseUrl,
    string? SupportEmail,
    string? DefaultLanguage,
    string? DefaultTimeZone,
    int MaxUploadSizeMb,
    int SessionTimeoutMinutes,
    Dictionary<string, string>? GlobalVariables);

public record SettingsDto(
    string Id,
    SystemSettingsDto SystemConfig,
    GlobalSettingsDto GlobalConfig,
    string UpdatedBy,
    DateTime? UpdatedAt);

// ── Commands / Queries ────────────────────────────────────────────────────
public record GetSettingsQuery : IRequest<ApiResponse<SettingsDto>>;

public record UpdateSettingsCommand(UpdateSettingsRequest Request, string? UpdatedBy = null) : IRequest<ApiResponse<SettingsDto>>;

public record ResetSettingsCommand(string? UpdatedBy = null) : IRequest<ApiResponse<SettingsDto>>;

public record UpdateSettingsRequest(SystemSettingsDto? SystemConfig, GlobalSettingsDto? GlobalConfig);

// ── Validators ────────────────────────────────────────────────────────────
public class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        When(x => x.Request.SystemConfig?.Sedentary is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Sedentary!.MaxSedentaryMinutes).InclusiveBetween(1, 600);
            RuleFor(x => x.Request.SystemConfig!.Sedentary!.MinActiveBreakMinutes).InclusiveBetween(1, 60);
        });

        When(x => x.Request.SystemConfig?.Sync is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Sync!.SyncIntervalMinutes).InclusiveBetween(1, 1440);
        });

        When(x => x.Request.SystemConfig?.Ai is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Ai!.ModelUpdateIntervalDays).InclusiveBetween(1, 365);
            RuleFor(x => x.Request.SystemConfig!.Ai!.ConfidenceThreshold).InclusiveBetween(0, 1);
            RuleFor(x => x.Request.SystemConfig!.Ai!.DataRetentionDays).InclusiveBetween(1, 3650);
            RuleFor(x => x.Request.SystemConfig!.Ai!.PredictionServiceUrl)
                .MaximumLength(500)
                .Must(u => string.IsNullOrWhiteSpace(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithMessage("PredictionServiceUrl must be an absolute URL.");
        });

        When(x => x.Request.SystemConfig?.Dashboard is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Dashboard!.RefreshIntervalSeconds).InclusiveBetween(5, 3600);
            RuleFor(x => x.Request.SystemConfig!.Dashboard!.CacheExpirationMinutes).InclusiveBetween(1, 1440);
            RuleFor(x => x.Request.SystemConfig!.Dashboard!.MaxWidgetsPerUser).InclusiveBetween(1, 100);
        });

        When(x => x.Request.SystemConfig?.Reports is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Reports!.MaxReportDays).InclusiveBetween(1, 3650);
            RuleFor(x => x.Request.SystemConfig!.Reports!.DefaultExportFormat)
                .Must(f => string.IsNullOrWhiteSpace(f) || new[] { "PDF", "EXCEL", "CSV" }.Contains(f, StringComparer.OrdinalIgnoreCase))
                .WithMessage("DefaultExportFormat must be PDF, EXCEL or CSV.");
        });

        When(x => x.Request.SystemConfig?.Alerts is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Alerts!.MaxAlertsPerDay).InclusiveBetween(1, 1000);
        });

        When(x => x.Request.SystemConfig?.Email is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Email!.SmtpPort).InclusiveBetween(1, 65535);
            RuleFor(x => x.Request.SystemConfig!.Email!.FromEmail)
                .EmailAddress().When(e => !string.IsNullOrWhiteSpace(e.Request.SystemConfig!.Email!.FromEmail));
        });

        When(x => x.Request.SystemConfig?.Push is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Push!.MaxPushPerDay).InclusiveBetween(1, 1000);
        });

        When(x => x.Request.SystemConfig?.Notifications is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Notifications!.DigestHour).InclusiveBetween(0, 23);
            RuleFor(x => x.Request.SystemConfig!.Notifications!.MaxNotificationsPerDay).InclusiveBetween(1, 10000);
        });

        When(x => x.Request.SystemConfig?.Saas is not null, () =>
        {
            RuleFor(x => x.Request.SystemConfig!.Saas!.TrialDays).InclusiveBetween(0, 365);
            RuleFor(x => x.Request.SystemConfig!.Saas!.MaxOrganizationsPerAccount).InclusiveBetween(1, 1000);
        });

        When(x => x.Request.GlobalConfig is not null, () =>
        {
            RuleFor(x => x.Request.GlobalConfig!.MaxUploadSizeMb).InclusiveBetween(1, 10240);
            RuleFor(x => x.Request.GlobalConfig!.SessionTimeoutMinutes).InclusiveBetween(5, 1440);
            RuleFor(x => x.Request.GlobalConfig!.GlobalVariables)
                .Must(v => v == null || v.Count <= 500)
                .WithMessage("GlobalVariables cannot contain more than 500 entries.");
        });
    }
}

// ── Command Handler ───────────────────────────────────────────────────────
public class SettingsCommandHandler :
    IRequestHandler<UpdateSettingsCommand, ApiResponse<SettingsDto>>,
    IRequestHandler<ResetSettingsCommand, ApiResponse<SettingsDto>>
{
    public const string SettingsCacheKey = "admin:settings:v1";

    private readonly IRepository<SystemConfiguration> _systemConfigRepository;
    private readonly IRepository<GlobalConfiguration> _globalConfigRepository;
    private readonly ICacheService _cache;

    public SettingsCommandHandler(
        IRepository<SystemConfiguration> systemConfigRepository,
        IRepository<GlobalConfiguration> globalConfigRepository,
        ICacheService cache)
    {
        _systemConfigRepository = systemConfigRepository;
        _globalConfigRepository = globalConfigRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<SettingsDto>> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var updatedBy = request.UpdatedBy ?? "system";

        var systemConfig = await GetOrCreateSystemConfigAsync(cancellationToken);
        if (request.Request.SystemConfig is { } sys)
        {
            systemConfig.Apply(
                Map(sys.Sedentary), Map(sys.Sync), Map(sys.Ai), Map(sys.Dashboard),
                Map(sys.Reports), Map(sys.Alerts), Map(sys.Email), Map(sys.Push),
                Map(sys.Notifications), Map(sys.Saas), Map(sys.Rules), updatedBy);
            await _systemConfigRepository.UpdateAsync(systemConfig, cancellationToken);
        }

        var globalConfig = await GetOrCreateGlobalConfigAsync(cancellationToken);
        if (request.Request.GlobalConfig is { } glob)
        {
            globalConfig.Apply(
                glob.ApplicationName ?? globalConfig.ApplicationName,
                glob.FrontendBaseUrl ?? globalConfig.FrontendBaseUrl,
                glob.SupportEmail ?? globalConfig.SupportEmail,
                glob.DefaultLanguage ?? globalConfig.DefaultLanguage,
                glob.DefaultTimeZone ?? globalConfig.DefaultTimeZone,
                glob.MaxUploadSizeMb,
                glob.SessionTimeoutMinutes,
                glob.GlobalVariables ?? globalConfig.GlobalVariables,
                updatedBy);
            await _globalConfigRepository.UpdateAsync(globalConfig, cancellationToken);
        }

        await _cache.RemoveAsync(SettingsCacheKey, cancellationToken);

        return ApiResponse<SettingsDto>.Ok(
            BuildDto(systemConfig, globalConfig),
            "Settings updated successfully.");
    }

    public async Task<ApiResponse<SettingsDto>> Handle(ResetSettingsCommand request, CancellationToken cancellationToken)
    {
        var updatedBy = request.UpdatedBy ?? "system";

        var systemConfig = await GetOrCreateSystemConfigAsync(cancellationToken);
        systemConfig.ResetToDefaults(updatedBy);
        await _systemConfigRepository.UpdateAsync(systemConfig, cancellationToken);

        var globalConfig = await GetOrCreateGlobalConfigAsync(cancellationToken);
        globalConfig.ResetToDefaults(updatedBy);
        await _globalConfigRepository.UpdateAsync(globalConfig, cancellationToken);

        await _cache.RemoveAsync(SettingsCacheKey, cancellationToken);

        return ApiResponse<SettingsDto>.Ok(
            BuildDto(systemConfig, globalConfig),
            "Settings restored to defaults.");
    }

    internal async Task<SystemConfiguration> GetOrCreateSystemConfigAsync(CancellationToken cancellationToken)
    {
        var config = await _systemConfigRepository.GetByIdAsync(SystemConfiguration.SingletonId, cancellationToken);
        if (config != null) return config;

        config = SystemConfiguration.CreateDefaults();
        await _systemConfigRepository.AddAsync(config, cancellationToken);
        return config;
    }

    internal async Task<GlobalConfiguration> GetOrCreateGlobalConfigAsync(CancellationToken cancellationToken)
    {
        var config = await _globalConfigRepository.GetByIdAsync(GlobalConfiguration.SingletonId, cancellationToken);
        if (config != null) return config;

        config = GlobalConfiguration.CreateDefaults();
        await _globalConfigRepository.AddAsync(config, cancellationToken);
        return config;
    }

    private static SettingsDto BuildDto(SystemConfiguration system, GlobalConfiguration global)
        => new(
            system.Id,
            new SystemSettingsDto(
                Map(system.Sedentary), Map(system.Sync), Map(system.Ai), Map(system.Dashboard),
                Map(system.Reports), Map(system.Alerts), Map(system.Email), Map(system.Push),
                Map(system.Notifications), Map(system.Saas), Map(system.Rules)),
            new GlobalSettingsDto(
                global.ApplicationName, global.FrontendBaseUrl, global.SupportEmail,
                global.DefaultLanguage, global.DefaultTimeZone, global.MaxUploadSizeMb,
                global.SessionTimeoutMinutes, global.GlobalVariables),
            system.UpdatedBy,
            system.UpdatedAt);

    // ── Section mappers (domain ⇄ DTO) ─────────────────────────────────────
    private static SedentarySettingsDto? Map(SedentarySettings? s) => s is null ? null : new(s.MaxSedentaryMinutes, s.MinActiveBreakMinutes);
    private static SyncSettingsDto? Map(SyncSettings? s) => s is null ? null : new(s.SyncIntervalMinutes);
    private static AiSettingsDto? Map(AiSettings? s) => s is null ? null : new(s.Enabled, s.PredictionServiceUrl, s.ModelUpdateIntervalDays, s.ConfidenceThreshold, s.DataRetentionDays);
    private static DashboardSettingsDto? Map(DashboardSettings? s) => s is null ? null : new(s.RefreshIntervalSeconds, s.CacheExpirationMinutes, s.MaxWidgetsPerUser);
    private static ReportsSettingsDto? Map(ReportSettings? s) => s is null ? null : new(s.Enabled, s.MaxReportDays, s.DefaultExportFormat);
    private static AlertsSettingsDto? Map(AlertSettings? s) => s is null ? null : new(s.Enabled, s.MaxAlertsPerDay);
    private static EmailSettingsDto? Map(EmailSettings? s) => s is null ? null : new(s.Enabled, s.FromEmail, s.SmtpHost, s.SmtpPort, s.RequireSsl);
    private static PushSettingsDto? Map(PushSettings? s) => s is null ? null : new(s.Enabled, s.MaxPushPerDay, s.WearEnabled);
    private static NotificationSettingsDto? Map(NotificationSettings? s) => s is null ? null : new(s.DigestEnabled, s.DigestHour, s.MaxNotificationsPerDay);
    private static SaasSettingsDto? Map(SaasSettings? s) => s is null ? null : new(s.AllowSelfSignup, s.TrialDays, s.MaxOrganizationsPerAccount);
    private static SystemRulesSettingsDto? Map(SystemRulesSettings? s) => s is null ? null : new(s.IdleThresholdMinutes, s.MinActiveBreakMinutes, s.MaxSedentaryStreakDays, s.AllowAnonymousAccess, s.DefaultLanguage, s.DefaultTimeZone);

    private static SedentarySettings? Map(SedentarySettingsDto? s) => s is null ? null : new SedentarySettings { MaxSedentaryMinutes = s.MaxSedentaryMinutes, MinActiveBreakMinutes = s.MinActiveBreakMinutes };
    private static SyncSettings? Map(SyncSettingsDto? s) => s is null ? null : new SyncSettings { SyncIntervalMinutes = s.SyncIntervalMinutes };
    private static AiSettings? Map(AiSettingsDto? s) => s is null ? null : new AiSettings { Enabled = s.Enabled, PredictionServiceUrl = s.PredictionServiceUrl, ModelUpdateIntervalDays = s.ModelUpdateIntervalDays, ConfidenceThreshold = s.ConfidenceThreshold, DataRetentionDays = s.DataRetentionDays };
    private static DashboardSettings? Map(DashboardSettingsDto? s) => s is null ? null : new DashboardSettings { RefreshIntervalSeconds = s.RefreshIntervalSeconds, CacheExpirationMinutes = s.CacheExpirationMinutes, MaxWidgetsPerUser = s.MaxWidgetsPerUser };
    private static ReportSettings? Map(ReportsSettingsDto? s) => s is null ? null : new ReportSettings { Enabled = s.Enabled, MaxReportDays = s.MaxReportDays, DefaultExportFormat = s.DefaultExportFormat };
    private static AlertSettings? Map(AlertsSettingsDto? s) => s is null ? null : new AlertSettings { Enabled = s.Enabled, MaxAlertsPerDay = s.MaxAlertsPerDay };
    private static EmailSettings? Map(EmailSettingsDto? s) => s is null ? null : new EmailSettings { Enabled = s.Enabled, FromEmail = s.FromEmail, SmtpHost = s.SmtpHost, SmtpPort = s.SmtpPort, RequireSsl = s.RequireSsl };
    private static PushSettings? Map(PushSettingsDto? s) => s is null ? null : new PushSettings { Enabled = s.Enabled, MaxPushPerDay = s.MaxPushPerDay, WearEnabled = s.WearEnabled };
    private static NotificationSettings? Map(NotificationSettingsDto? s) => s is null ? null : new NotificationSettings { DigestEnabled = s.DigestEnabled, DigestHour = s.DigestHour, MaxNotificationsPerDay = s.MaxNotificationsPerDay };
    private static SaasSettings? Map(SaasSettingsDto? s) => s is null ? null : new SaasSettings { AllowSelfSignup = s.AllowSelfSignup, TrialDays = s.TrialDays, MaxOrganizationsPerAccount = s.MaxOrganizationsPerAccount };
    private static SystemRulesSettings? Map(SystemRulesSettingsDto? s) => s is null ? null : new SystemRulesSettings { IdleThresholdMinutes = s.IdleThresholdMinutes, MinActiveBreakMinutes = s.MinActiveBreakMinutes, MaxSedentaryStreakDays = s.MaxSedentaryStreakDays, AllowAnonymousAccess = s.AllowAnonymousAccess, DefaultLanguage = s.DefaultLanguage, DefaultTimeZone = s.DefaultTimeZone };
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class SettingsQueryHandler : IRequestHandler<GetSettingsQuery, ApiResponse<SettingsDto>>
{
    private readonly IRepository<SystemConfiguration> _systemConfigRepository;
    private readonly IRepository<GlobalConfiguration> _globalConfigRepository;
    private readonly ICacheService _cache;

    public SettingsQueryHandler(
        IRepository<SystemConfiguration> systemConfigRepository,
        IRepository<GlobalConfiguration> globalConfigRepository,
        ICacheService cache)
    {
        _systemConfigRepository = systemConfigRepository;
        _globalConfigRepository = globalConfigRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<SettingsDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<SettingsDto>(SettingsCommandHandler.SettingsCacheKey, cancellationToken);
        if (cached != null)
        {
            return ApiResponse<SettingsDto>.Ok(cached);
        }

        var systemConfig = await _systemConfigRepository.GetByIdAsync(SystemConfiguration.SingletonId, cancellationToken);
        var globalConfig = await _globalConfigRepository.GetByIdAsync(GlobalConfiguration.SingletonId, cancellationToken);

        if (systemConfig == null)
        {
            systemConfig = SystemConfiguration.CreateDefaults();
            await _systemConfigRepository.AddAsync(systemConfig, cancellationToken);
        }

        if (globalConfig == null)
        {
            globalConfig = GlobalConfiguration.CreateDefaults();
            await _globalConfigRepository.AddAsync(globalConfig, cancellationToken);
        }

        var dto = BuildDto(systemConfig, globalConfig);
        await _cache.SetAsync(SettingsCommandHandler.SettingsCacheKey, dto, TimeSpan.FromSeconds(30), cancellationToken);

        return ApiResponse<SettingsDto>.Ok(dto);
    }

    private static SettingsDto BuildDto(SystemConfiguration system, GlobalConfiguration global)
        => new(
            system.Id,
            new SystemSettingsDto(
                Map(system.Sedentary), Map(system.Sync), Map(system.Ai), Map(system.Dashboard),
                Map(system.Reports), Map(system.Alerts), Map(system.Email), Map(system.Push),
                Map(system.Notifications), Map(system.Saas), Map(system.Rules)),
            new GlobalSettingsDto(
                global.ApplicationName, global.FrontendBaseUrl, global.SupportEmail,
                global.DefaultLanguage, global.DefaultTimeZone, global.MaxUploadSizeMb,
                global.SessionTimeoutMinutes, global.GlobalVariables),
            system.UpdatedBy,
            system.UpdatedAt);

    private static SedentarySettingsDto? Map(SedentarySettings? s) => s is null ? null : new(s.MaxSedentaryMinutes, s.MinActiveBreakMinutes);
    private static SyncSettingsDto? Map(SyncSettings? s) => s is null ? null : new(s.SyncIntervalMinutes);
    private static AiSettingsDto? Map(AiSettings? s) => s is null ? null : new(s.Enabled, s.PredictionServiceUrl, s.ModelUpdateIntervalDays, s.ConfidenceThreshold, s.DataRetentionDays);
    private static DashboardSettingsDto? Map(DashboardSettings? s) => s is null ? null : new(s.RefreshIntervalSeconds, s.CacheExpirationMinutes, s.MaxWidgetsPerUser);
    private static ReportsSettingsDto? Map(ReportSettings? s) => s is null ? null : new(s.Enabled, s.MaxReportDays, s.DefaultExportFormat);
    private static AlertsSettingsDto? Map(AlertSettings? s) => s is null ? null : new(s.Enabled, s.MaxAlertsPerDay);
    private static EmailSettingsDto? Map(EmailSettings? s) => s is null ? null : new(s.Enabled, s.FromEmail, s.SmtpHost, s.SmtpPort, s.RequireSsl);
    private static PushSettingsDto? Map(PushSettings? s) => s is null ? null : new(s.Enabled, s.MaxPushPerDay, s.WearEnabled);
    private static NotificationSettingsDto? Map(NotificationSettings? s) => s is null ? null : new(s.DigestEnabled, s.DigestHour, s.MaxNotificationsPerDay);
    private static SaasSettingsDto? Map(SaasSettings? s) => s is null ? null : new(s.AllowSelfSignup, s.TrialDays, s.MaxOrganizationsPerAccount);
    private static SystemRulesSettingsDto? Map(SystemRulesSettings? s) => s is null ? null : new(s.IdleThresholdMinutes, s.MinActiveBreakMinutes, s.MaxSedentaryStreakDays, s.AllowAnonymousAccess, s.DefaultLanguage, s.DefaultTimeZone);
}
