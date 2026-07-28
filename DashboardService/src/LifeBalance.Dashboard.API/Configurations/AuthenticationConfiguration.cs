using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddDashboardAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT authentication is configured in Infrastructure AddInfrastructureServices
        return services;
    }
}
