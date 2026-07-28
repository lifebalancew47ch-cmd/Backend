using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs a warning when a request takes longer than
/// a configured threshold (default: 500ms). Useful for identifying performance bottlenecks.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int WarningThresholdMs = 500;

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        timer.Stop();

        if (timer.ElapsedMilliseconds > WarningThresholdMs)
        {
            logger.LogWarning(
                "Long-running request detected: [{RequestName}] took {ElapsedMs}ms. {@Request}",
                typeof(TRequest).Name,
                timer.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}
