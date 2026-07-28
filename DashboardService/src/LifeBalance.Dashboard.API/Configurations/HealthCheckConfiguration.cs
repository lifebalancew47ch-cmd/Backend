using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddDashboardHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddMongoDb(
                mongodbConnectionString: configuration.GetConnectionString("MongoDB")
                    ?? configuration["MongoDb:ConnectionString"]
                    ?? "mongodb://localhost:27017",
                name: "mongodb",
                tags: new[] { "ready", "db" });

        return services;
    }
}
