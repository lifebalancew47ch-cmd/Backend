using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.API.Configurations;
using LifeBalance.Dashboard.API.HealthChecks;
using LifeBalance.Dashboard.API.Middlewares;
using LifeBalance.Dashboard.API.Services;
using LifeBalance.Dashboard.Application;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Text.Json.Serialization;

// ============================================================
// Bootstrap Serilog as early as possible (before Host builds)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LifeBalance.Dashboard.API");

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
    builder.Services.AddInfrastructureServices(builder.Configuration);

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
    // Swagger / OpenAPI
    // --------------------------------------------------------
    builder.Services.AddDashboardSwagger();

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

        options.AddPolicy(Policies.DashboardRead, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(Roles.User, Roles.Admin, Roles.Viewer));

        options.AddPolicy(Policies.DashboardWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireRole(Roles.User, Roles.Admin));
    });

    // --------------------------------------------------------
    // Health Checks
    // --------------------------------------------------------
    builder.Services
        .AddHealthChecks()
        .AddMongoDb(
            sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>(),
            name:  "mongodb",
            tags:  new[] { "ready", "db" });

    // --------------------------------------------------------
    // CORS
    // --------------------------------------------------------
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DashboardCorsPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
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
    // Rate Limiting
    // --------------------------------------------------------
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", limiterOptions =>
        {
            limiterOptions.Window            = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit       = 100;
            limiterOptions.QueueLimit        = 0;
            limiterOptions.QueueProcessingOrder =
                System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
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

    // 5. Swagger (all environments for now; restrict in production as needed)
    app.UseDashboardSwagger();

    // 6. Response compression & caching
    app.UseResponseCompression();
    app.UseResponseCaching();

    // 7. CORS
    app.UseCors("DashboardCorsPolicy");

    // 8. Rate limiting
    app.UseRateLimiter();

    // 9. Routing
    app.UseRouting();

    // 10. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 11. Map endpoints
    app.MapControllers();
    app.MapDashboardHealthChecks();

    Log.Information("LifeBalance.Dashboard.API started successfully");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "LifeBalance.Dashboard.API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }

