using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Infrastructure.Configuration;
using LifeBalance.Notifications.Infrastructure.Data;
using LifeBalance.Notifications.Infrastructure.Services;
using LifeBalance.Notifications.Presentation.Middlewares;
using Microsoft.Extensions.Hosting;

namespace LifeBalance.Notifications.Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // DevSecOps Rule 6 (fail-fast HTTPS): never start outside Development with outbound
        // service URLs using http:// — credentials/telemetry could leak in clear text and a
        // caller could be silently downgraded to a plaintext endpoint.
        if (!environment.IsDevelopment())
        {
            var externalServices = configuration.GetSection("ExternalServices");
            foreach (var service in externalServices.GetChildren())
            {
                var url = service.Value;
                if (!string.IsNullOrWhiteSpace(url) &&
                    url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"ExternalServices:{service.Key} must use HTTPS in non-development environments. Current value: '{url}'.");
                }
            }
        }

        services.AddSingleton<MongoDbContext>();
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPushService, PushService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();

        services.AddScoped<IPushNotificationProvider, FirebasePushProvider>();

        services.AddHttpClient<IAuthServiceClient, AuthServiceClient>();
        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>();
        services.AddHttpClient<ISedentaryServiceClient, SedentaryServiceClient>();
        services.AddHttpClient<IMLServiceClient, MLServiceClient>();
        services.AddHttpClient<IAdminServiceClient, AdminServiceClient>();
        services.AddHttpClient<IDashboardServiceClient, DashboardServiceClient>();

        services.AddHostedService<ScheduledNotificationWorker>();

        return services;
    }

    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
