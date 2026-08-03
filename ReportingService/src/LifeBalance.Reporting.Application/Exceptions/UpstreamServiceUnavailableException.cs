namespace LifeBalance.Reporting.Application.Exceptions;

/// <summary>
/// Exception raised when a required upstream microservice is unreachable or
/// does not return the expected data. Converted to a 503 Service Unavailable
/// response by the global exception middleware (fail closed, never fabricated data).
/// </summary>
public sealed class UpstreamServiceUnavailableException : Exception
{
    /// <summary>Initializes a new instance of <see cref="UpstreamServiceUnavailableException"/>.</summary>
    public UpstreamServiceUnavailableException(string message)
        : base(message)
    {
    }
}
