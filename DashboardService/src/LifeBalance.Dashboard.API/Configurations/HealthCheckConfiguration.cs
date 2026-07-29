using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddDashboardHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddMongoDb(
                sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>(),
                name: "mongodb",
                tags: new[] { "ready", "db" });

        return services;
    }
}
