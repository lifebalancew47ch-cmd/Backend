namespace LifeBalance.Dashboard.Contracts.Common;

/// <summary>
/// Standard API error response body.
/// Conforms to RFC 7807 Problem Details.
/// </summary>
/// <param name="Type">A URI reference that identifies the problem type.</param>
/// <param name="Title">A short, human-readable summary.</param>
/// <param name="Status">The HTTP status code.</param>
/// <param name="Detail">A human-readable explanation specific to this occurrence.</param>
/// <param name="Instance">A URI reference that identifies the specific occurrence.</param>
/// <param name="TraceId">The distributed trace identifier for correlation.</param>
public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string? Instance = null,
    string? TraceId = null);
