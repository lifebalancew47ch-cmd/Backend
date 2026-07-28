using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class CachingConfiguration
{
    public static IServiceCollection AddDashboardCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddResponseCaching();
        return services;
    }
}
