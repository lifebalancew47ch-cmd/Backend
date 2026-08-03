namespace LifeBalance.Reporting.API.Middlewares;

/// <summary>
/// Applies OWASP recommended security headers to every response.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of <see cref="SecurityHeadersMiddleware"/>.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <inheritdoc cref="RequestDelegate"/>
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Frame-Options"] = "DENY";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; img-src 'self' data:;";

        await _next(context);
    }
}
