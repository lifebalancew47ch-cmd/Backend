using System.Text.Json.Serialization;

namespace LifeBalance.Reporting.Shared.Results;

/// <summary>
/// Uniform response wrapper for all Reporting Service API responses.
/// Follows the LifeBalance common response contract: <c>Response&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Indicates if the request was processed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>User-friendly message regarding the operation.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The response payload.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>List of validation or processing error messages if any.</summary>
    public List<string>? Errors { get; set; }

    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>Unique Correlation ID associated with this request execution trace.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>UTC timestamp of response creation.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Request executed successfully.", string traceId = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>Creates a failed response.</summary>
    public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400, string traceId = "")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string> { message },
            StatusCode = statusCode,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }
}
