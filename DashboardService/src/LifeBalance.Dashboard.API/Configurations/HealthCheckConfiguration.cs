using System;
using LifeBalance.Dashboard.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddDashboardHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>() ?? new ServiceUrlsOptions();

        services.AddHealthChecks()
            .AddMongoDb(
                sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>(),
                name: "mongodb",
                tags: new[] { "ready", "db" })
            .AddUrlGroup(new Uri($"{serviceUrls.AuthServiceUrl}/health"), name: "AuthService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.MedicalDataServiceUrl}/health"), name: "MedicalDataService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.SedentaryEngineServiceUrl}/health"), name: "SedentaryEngineService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.GamificationServiceUrl}/health"), name: "GamificationService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.NotificationServiceUrl}/health"), name: "NotificationService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.MlPredictionServiceUrl}/health"), name: "MlPredictionService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.OrganizationServiceUrl}/health"), name: "OrganizationService", tags: new[] { "ready", "upstream" })
            .AddUrlGroup(new Uri($"{serviceUrls.ReportingServiceUrl}/health"), name: "ReportingService", tags: new[] { "ready", "upstream" });

        return services;
    }
}
