using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Log entry ingested from any LifeBalance microservice. The Administration
/// Service centralises logs so operators can inspect the whole platform in one
/// place without direct database access.
/// </summary>
public class SystemLog : AggregateRoot
{
    public MicroserviceName Service { get; private set; } = MicroserviceName.Auth;
    public SystemLogLevel Level { get; private set; } = SystemLogLevel.Information;
    public string Message { get; private set; } = string.Empty;
    public string? Exception { get; private set; }
    public string? StackTrace { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private SystemLog() { }

    public SystemLog(MicroserviceName service,
                     SystemLogLevel level,
                     string message,
                     string? exception = null,
                     string? stackTrace = null,
                     string source = "",
                     string? userId = null,
                     string? correlationId = null,
                     DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Log message is required.", nameof(message));

        Service = service;
        Level = level;
        Message = message.Trim();
        Exception = exception;
        StackTrace = stackTrace;
        Source = source;
        UserId = userId;
        CorrelationId = correlationId ?? string.Empty;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }
}
