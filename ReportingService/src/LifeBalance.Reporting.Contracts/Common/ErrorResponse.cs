namespace LifeBalance.Reporting.Contracts.Common;

/// <summary>
/// Standard error response envelope used when a request fails.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>Gets or sets the HTTP status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>Gets or sets a short, machine-readable error code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets optional validation error details.</summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>Gets or sets the correlation identifier of the failed request.</summary>
    public string TraceId { get; set; } = string.Empty;
}
