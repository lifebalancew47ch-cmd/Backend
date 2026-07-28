using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Domain.Repositories;
using LifeBalance.Dashboard.Infrastructure.HttpClients;
using LifeBalance.Dashboard.Infrastructure.HttpClients.DelegatingHandlers;
using LifeBalance.Dashboard.Infrastructure.Options;
using LifeBalance.Dashboard.Infrastructure.Persistence.Mongo;
using LifeBalance.Dashboard.Infrastructure.Persistence.Repositories;
using LifeBalance.Dashboard.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Text;

namespace LifeBalance.Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ------ Options ------
        services.Configure<MongoDbOptions>(
            configuration.GetSection(MongoDbOptions.SectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<HttpClientOptions>(
            configuration.GetSection(HttpClientOptions.SectionName));

        services.Configure<ServiceUrlsOptions>(
            configuration.GetSection(ServiceUrlsOptions.SectionName));

        // ------ MongoDB Context & Repositories ------
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IDashboardCacheRepository, DashboardCacheRepository>();
        services.AddScoped<IAggregationLogRepository, AggregationLogRepository>();

        // ------ Services ------
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDashboardCacheService, DashboardCacheService>();

        // ------ Delegating Handlers ------
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<JwtPropagationDelegatingHandler>();

        // ------ JWT Authentication ------
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtOptions.Issuer,
                    ValidAudience            = jwtOptions.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        // ------ HTTP Clients Registration with Polly Resilience ------
        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>() ?? new ServiceUrlsOptions();

        RegisterTypedClient<IAuthServiceClient, AuthServiceClient>(services, serviceUrls.AuthServiceUrl);
        RegisterTypedClient<IMedicalDataServiceClient, MedicalDataServiceClient>(services, serviceUrls.MedicalDataServiceUrl);
        RegisterTypedClient<ISedentaryEngineServiceClient, SedentaryEngineServiceClient>(services, serviceUrls.SedentaryEngineServiceUrl);
        RegisterTypedClient<IGamificationServiceClient, GamificationServiceClient>(services, serviceUrls.GamificationServiceUrl);
        RegisterTypedClient<INotificationServiceClient, NotificationServiceClient>(services, serviceUrls.NotificationServiceUrl);
        RegisterTypedClient<IMlPredictionServiceClient, MlPredictionServiceClient>(services, serviceUrls.MlPredictionServiceUrl);
        RegisterTypedClient<IOrganizationServiceClient, OrganizationServiceClient>(services, serviceUrls.OrganizationServiceUrl);
        RegisterTypedClient<IReportingServiceClient, ReportingServiceClient>(services, serviceUrls.ReportingServiceUrl);

        return services;
    }

    private static void RegisterTypedClient<TInterface, TImplementation>(IServiceCollection services, string baseUrl)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddHttpMessageHandler<JwtPropagationDelegatingHandler>()
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
