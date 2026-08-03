using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request start, completion, and duration.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("[{RequestName}] Handling request", requestName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "[{RequestName}] Completed in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "[{RequestName}] Failed after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
