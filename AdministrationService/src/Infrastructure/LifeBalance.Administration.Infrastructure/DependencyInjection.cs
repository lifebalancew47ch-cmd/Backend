using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Interfaces;
using LifeBalance.Administration.Infrastructure.ExternalServices;
using LifeBalance.Administration.Infrastructure.Persistence;
using LifeBalance.Administration.Infrastructure.Services;

namespace LifeBalance.Administration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // 1. MongoDB setup
        var mongoConnection = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var mongoDbName = configuration["DatabaseSettings:DatabaseName"] ?? "LifeBalance_Administration";
        services.AddSingleton(sp => new MongoDbContext(mongoConnection, mongoDbName));
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // 2. Cache (in-memory distributed cache; Redis optional)
        services.AddDistributedMemoryCache();
        services.AddSingleton<ICacheService, DistributedCacheService>();

        // 3. HTTP context + current admin user + audit
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();

        // 4. Service monitoring aggregator
        services.AddScoped<IServiceStatusService, ServiceStatusService>();

        // 5. External clients (HttpClientFactory + Polly resilience)
        RegisterExternalClients(services, configuration, environment);

        return services;
    }

    private static void RegisterExternalClients(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // The token propagation handler is resolved from DI by AddHttpMessageHandler.
        services.AddTransient<BearerTokenPropagationHandler>();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        RegisterClient<IAuthProfileServiceClient, AuthProfileServiceClient>(services, configuration, environment, "AuthProfileUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<IOrganizationServiceClient, OrganizationServiceClient>(services, configuration, environment, "OrganizationUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<INotificationServiceClient, NotificationServiceClient>(services, configuration, environment, "NotificationUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<IMedicalDataServiceClient, MedicalDataServiceClient>(services, configuration, environment, "MedicalDataUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<ISedentaryEngineServiceClient, SedentaryEngineServiceClient>(services, configuration, environment, "SedentaryEngineUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<IDashboardServiceClient, DashboardServiceClient>(services, configuration, environment, "DashboardUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<IReportingServiceClient, ReportingServiceClient>(services, configuration, environment, "ReportingUrl", retryPolicy, circuitBreakerPolicy);
        RegisterClient<IMLPredictionServiceClient, MLPredictionServiceClient>(services, configuration, environment, "MlPredictionUrl", retryPolicy, circuitBreakerPolicy);
    }

    private static void RegisterClient<TInterface, TImplementation>(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string configKey,
        IAsyncPolicy<HttpResponseMessage> retryPolicy,
        IAsyncPolicy<HttpResponseMessage> circuitBreaker)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var baseUrl = configuration[$"Microservices:{configKey}"] ?? $"http://localhost:{DefaultPortFor(configKey)}";

        // Rule: outbound traffic MUST be HTTPS outside Development.
        if (!environment.IsDevelopment() && baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Microservices:{configKey} must use HTTPS in non-development environments. Current value: '{baseUrl}'.");
        }

        services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(8);
        })
        .AddHttpMessageHandler<BearerTokenPropagationHandler>()
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreaker);
    }

    private static int DefaultPortFor(string configKey) => configKey switch
    {
        "AuthProfileUrl" => 5001,
        "DashboardUrl" => 5002,
        "ReportingUrl" => 5003,
        "NotificationUrl" => 5004,
        "MedicalDataUrl" => 5101,
        "SedentaryEngineUrl" => 5102,
        "MlPredictionUrl" => 5103,
        _ => 5005
    };
}
