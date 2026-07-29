using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LifeBalance.Dashboard.API.HealthChecks;

/// <summary>
/// Extension methods for configuring Health Check endpoints in the ASP.NET Core pipeline.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Maps liveness, readiness, and UI health check endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDashboardHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness — is the process alive?
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate      = _ => false,   // Only checks the host itself
            ResponseWriter = WriteJsonResponse
        });

        // Readiness — is the service ready to serve traffic?
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate      = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteJsonResponse
        });

        // Full — all health checks
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponse
        });

        return endpoints;
    }

    private static Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            @checked = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                name        = e.Key,
                status      = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration    = e.Value.Duration.TotalMilliseconds
            })
        });

        return context.Response.WriteAsync(result);
    }
}
