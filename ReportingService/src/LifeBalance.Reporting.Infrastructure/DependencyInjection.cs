using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.Repositories;
using LifeBalance.Reporting.Infrastructure.HttpClients;
using LifeBalance.Reporting.Infrastructure.HttpClients.DelegatingHandlers;
using LifeBalance.Reporting.Infrastructure.Options;
using LifeBalance.Reporting.Infrastructure.Persistence.Mongo;
using LifeBalance.Reporting.Infrastructure.Persistence.Repositories;
using LifeBalance.Reporting.Infrastructure.ReportGeneration;
using LifeBalance.Reporting.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace LifeBalance.Reporting.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure services: options, MongoDB, JWT authentication, typed HTTP
    /// clients with Polly resilience and the report document generators.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ------ Options ------
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<HttpClientOptions>(configuration.GetSection(HttpClientOptions.SectionName));
        services.Configure<ServiceUrlsOptions>(configuration.GetSection(ServiceUrlsOptions.SectionName));

        // ------ MongoDB Context & Repositories ------
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReportGenerationLogRepository, ReportGenerationLogRepository>();
        services.AddHostedService<MongoIndexInitializer>();

        // ------ Services ------
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IReportGenerationLogService, ReportGenerationLogService>();
        services.AddScoped<IHealthProbeService, HealthProbeService>();

        // ------ Report document generators ------
        services.AddSingleton<IPdfReportGenerator, PdfReportGenerator>();
        services.AddSingleton<IExcelReportGenerator, ExcelReportGenerator>();
        services.AddSingleton<ICsvReportGenerator, CsvReportGenerator>();

        // ------ Delegating Handlers ------
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<JwtPropagationDelegatingHandler>();

        // ------ JWT Authentication ------
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Fail-fast JWT: never start in Production with an empty, short or placeholder secret.
        if (environment.IsProduction() &&
            (string.IsNullOrEmpty(jwtOptions.SecretKey) ||
             Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32 ||
             jwtOptions.SecretKey == "CHANGE_THIS_TO_A_32_CHARACTER_SECRET_KEY_IN_PRODUCTION!!"))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must be configured with at least 32 bytes and cannot be the placeholder value in production.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });

        // ------ HTTP Clients Registration with Polly Resilience ------
        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>() ?? new ServiceUrlsOptions();

        RegisterTypedClient<IAuthServiceClient, AuthServiceClient>(services, serviceUrls.AuthServiceUrl, environment);
        RegisterTypedClient<IMedicalDataServiceClient, MedicalDataServiceClient>(services, serviceUrls.MedicalDataServiceUrl, environment);
        RegisterTypedClient<ISedentaryEngineServiceClient, SedentaryEngineServiceClient>(services, serviceUrls.SedentaryEngineServiceUrl, environment);
        RegisterTypedClient<IDashboardServiceClient, DashboardServiceClient>(services, serviceUrls.DashboardServiceUrl, environment);
        RegisterTypedClient<IOrganizationServiceClient, OrganizationServiceClient>(services, serviceUrls.OrganizationServiceUrl, environment);

        return services;
    }

    private static void RegisterTypedClient<TInterface, TImplementation>(
        IServiceCollection services,
        string baseUrl,
        IHostEnvironment environment)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var uri = new Uri(baseUrl);
        if (!environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Service URL '{baseUrl}' must use HTTPS outside the Development environment.");
        }

        services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
