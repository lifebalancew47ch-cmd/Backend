using Microsoft.AspNetCore.Http;

namespace LifeBalance.Notifications.Presentation.Middlewares;

/// <summary>
/// DevSecOps: aplica los OWASP recommended security headers a cada respuesta,
/// con una CSP relajada únicamente para las rutas de Swagger (Dev-only).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Frame-Options"] = "DENY";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            headers["Content-Security-Policy"] = "default-src 'self' 'unsafe-inline' 'unsafe-eval' data:;";
        }
        else
        {
            headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; img-src 'self' data:;";
        }

        await _next(context);
    }
}