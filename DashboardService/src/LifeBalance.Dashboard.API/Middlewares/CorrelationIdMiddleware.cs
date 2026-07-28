using LifeBalance.Dashboard.Shared.Constants;

namespace LifeBalance.Dashboard.API.Middlewares;

/// <summary>
/// Middleware that ensures every request has a Correlation ID.
/// Reads from the incoming <c>X-Correlation-ID</c> header or generates a new <see cref="Guid"/>.
/// Propagates the value in both the response header and the logging scope.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of <see cref="CorrelationIdMiddleware"/>.</summary>
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    /// <inheritdoc cref="RequestDelegate"/>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(SharedConstants.CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        // Enrich the logging scope
        using (context.RequestServices
            .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object>
            {
                [SharedConstants.CorrelationIdHeader] = correlationId
            }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                SharedConstants.CorrelationIdHeader, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId!;
        }

        return Guid.NewGuid().ToString();
    }
}
