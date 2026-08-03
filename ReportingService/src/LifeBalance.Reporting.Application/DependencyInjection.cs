using System.Reflection;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Services;
using LifeBalance.Reporting.Domain.DomainServices;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Reporting.Application;

/// <summary>
/// Extension methods for registering Application layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services: MediatR, AutoMapper, FluentValidation, pipeline
    /// behaviors, the statistical analyzer and the report data aggregator.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR - registers all handlers and pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors execute in registration order
            cfg.AddOpenBehavior(typeof(Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(Behaviors.PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
        });

        // AutoMapper - scans assembly for Profile classes
        services.AddAutoMapper(_ => { }, assembly);

        // FluentValidation - scans assembly for all AbstractValidator<T> classes
        services.AddValidatorsFromAssembly(assembly);

        // Domain analytics (pure logic, registered here for convenience)
        services.AddScoped<IStatisticalAnalyzer, StatisticalAnalyzer>();

        // Data consolidation & authorization for reports
        services.AddScoped<IReportDatasetService, ReportDataAggregator>();

        return services;
    }
}
