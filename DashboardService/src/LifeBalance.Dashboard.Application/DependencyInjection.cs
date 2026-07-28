using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LifeBalance.Dashboard.Application;

/// <summary>
/// Extension methods for registering Application layer services into the DI container.
/// Call from <c>Program.cs</c> or the API's <c>DependencyInjection</c> class.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services: MediatR, AutoMapper, FluentValidation, and pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — registers all Handlers, Notifications, and Pipeline Behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors execute in registration order
            cfg.AddOpenBehavior(typeof(Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(Behaviors.PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
        });

        // AutoMapper — scans assembly for Profile classes
        services.AddAutoMapper(assembly);

        // FluentValidation — scans assembly for all AbstractValidator<T> classes
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
