using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.API.Configurations;
using LifeBalance.Reporting.API.HealthChecks;
using LifeBalance.Reporting.API.Middlewares;
using LifeBalance.Reporting.API.Services;
using LifeBalance.Reporting.Application;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

// ============================================================
// Bootstrap Serilog as early as possible (before Host builds)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LifeBalance.Reporting.API");

    var builder = WebApplication.CreateBuilder(args);

    // --------------------------------------------------------
    // Serilog — read full config from appsettings
    // --------------------------------------------------------
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName());

    // --------------------------------------------------------
    // Application & Infrastructure Services
    // --------------------------------------------------------
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    // --------------------------------------------------------
    // Current User
    // --------------------------------------------------------
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // --------------------------------------------------------
    // Controllers & JSON
    // --------------------------------------------------------
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // --------------------------------------------------------
    // API Versioning
    // --------------------------------------------------------
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    // --------------------------------------------------------
    // Swagger / OpenAPI (Development only — Rule 7)
    // --------------------------------------------------------
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddReportingSwagger();
    }

    // --------------------------------------------------------
    // Authorization Policies
    // --------------------------------------------------------
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy(Policies.AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy(Policies.Admin, policy =>
            policy.RequireRole(Roles.Admin));

        options.AddPolicy(Policies.ReportRead, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(Roles.User, Roles.Admin, Roles.Viewer));

        options.AddPolicy(Policies.ReportExport, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(Roles.User, Roles.Admin));
    });

    // --------------------------------------------------------
    // Health Checks
    // --------------------------------------------------------
    builder.Services
        .AddHealthChecks()
        .AddCheck<MongoDbHealthCheck>(
            "mongodb",
            tags: new[] { "ready", "db" });

    // --------------------------------------------------------
    // CORS — origins from configuration (Cors:AllowedOrigins)
    // --------------------------------------------------------
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReportingCorsPolicy", policy =>
        {
            if (allowedOrigins is { Length: > 0 })
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(Array.Empty<string>())
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // --------------------------------------------------------
    // Response Compression
    // --------------------------------------------------------
    builder.Services.AddResponseCompression(options =>
        options.EnableForHttps = true);

    // --------------------------------------------------------
    // Memory Cache & Response Caching
    // --------------------------------------------------------
    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCaching();

    // --------------------------------------------------------
    // Rate Limiting — fixed window partitioned per client IP
    // --------------------------------------------------------
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("fixed", context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString()
                ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 100,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
    });

    // --------------------------------------------------------
    // Problem Details
    // --------------------------------------------------------
    builder.Services.AddProblemDetails();

    // ============================================================
    // Build the Application
    // ============================================================
    var app = builder.Build();

    // --------------------------------------------------------
    // Middleware pipeline
    // --------------------------------------------------------

    // 1. Exception handling — must be first
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 2. Correlation ID propagation
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 3. OWASP Security Headers
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 3. HTTPS redirection & HSTS
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // 4. Serilog request logging
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    // 5. Swagger (Development only — Rule 7)
    if (app.Environment.IsDevelopment())
    {
        app.UseReportingSwagger();
    }

    // 6. Response compression & caching
    app.UseResponseCompression();
    app.UseResponseCaching();

    // 7. CORS
    app.UseCors("ReportingCorsPolicy");

    // 8. Rate limiting
    app.UseRateLimiter();

    // 9. Routing
    app.UseRouting();

    // 10. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 11. Map endpoints
    app.MapControllers();
    app.MapReportingHealthChecks();

    Log.Information("LifeBalance.Reporting.API started successfully");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "LifeBalance.Reporting.API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
