using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Infrastructure.ExternalServices;
using LifeBalance.OrganizationSaaS.Infrastructure.Persistence;
using LifeBalance.OrganizationSaaS.Infrastructure.Services;

namespace LifeBalance.OrganizationSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Mongo DB Setup
        var mongoConn = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var mongoDbName = configuration["DatabaseSettings:DatabaseName"] ?? "LifeBalance_OrganizationSaaS";
        services.AddSingleton(new MongoDbContext(mongoConn, mongoDbName));
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // 2. Tenant Context & HTTP Context Accessor
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContextAccessor>();

        // 3. Cache Setup
        services.AddDistributedMemoryCache(); // Fallback in-memory distributed cache
        services.AddSingleton<ICacheService, DistributedCacheService>();

        // 4. Polly Resiliency Policy for Http Clients
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // 5. External HTTP Clients Registration
        RegisterHttpClient<IAuthProfileServiceClient, AuthProfileServiceClient>(services, configuration["Microservices:AuthProfileUrl"] ?? "http://localhost:5001", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<IDashboardServiceClient, DashboardServiceClient>(services, configuration["Microservices:DashboardUrl"] ?? "http://localhost:5002", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<IReportingServiceClient, ReportingServiceClient>(services, configuration["Microservices:ReportingUrl"] ?? "http://localhost:5003", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<INotificationServiceClient, NotificationServiceClient>(services, configuration["Microservices:NotificationUrl"] ?? "http://localhost:5004", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<IGamificationServiceClient, GamificationServiceClient>(services, configuration["Microservices:GamificationUrl"] ?? "http://localhost:5005", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<IMLPredictionServiceClient, MLPredictionServiceClient>(services, configuration["Microservices:MlPredictionUrl"] ?? "http://localhost:5006", retryPolicy, circuitBreakerPolicy);
        RegisterHttpClient<IAdministrationServiceClient, AdministrationServiceClient>(services, configuration["Microservices:AdministrationUrl"] ?? "http://localhost:5007", retryPolicy, circuitBreakerPolicy);

        return services;
    }

    private static void RegisterHttpClient<TInterface, TImplementation>(
        IServiceCollection services,
        string baseUrl,
        IAsyncPolicy<HttpResponseMessage> retryPolicy,
        IAsyncPolicy<HttpResponseMessage> circuitBreaker)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreaker);
    }
}
