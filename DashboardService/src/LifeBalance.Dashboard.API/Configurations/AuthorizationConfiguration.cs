using LifeBalance.Dashboard.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.API.Configurations;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddDashboardAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(Policies.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(Policies.Admin, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.DashboardRead, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(Roles.User, Roles.Admin, Roles.Viewer));

            options.AddPolicy(Policies.DashboardWrite, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(Roles.User, Roles.Admin));
        });

        return services;
    }
}
