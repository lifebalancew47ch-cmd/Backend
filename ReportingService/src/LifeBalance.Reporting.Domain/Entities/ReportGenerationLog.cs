using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.Domain.Entities;

/// <summary>
/// Audit entity that records every report generation request handled by the service.
/// The Reporting service stores no business data; this log is used only for
/// observability, billing/usage analysis and the "history" endpoint.
/// </summary>
public sealed class ReportGenerationLog
{
    /// <summary>Gets or sets the MongoDB ObjectId.</summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Gets or sets the identifier of the user who requested the report.</summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scope of the report.</summary>
    [BsonElement("scope")]
    [BsonRepresentation(BsonType.String)]
    public ReportScope Scope { get; set; }

    /// <summary>Gets or sets the resolved scope identifier (userId / familyId / companyId).</summary>
    [BsonElement("scopeId")]
    public string? ScopeId { get; set; }

    /// <summary>Gets or sets the requested output format (null for JSON analytics).</summary>
    [BsonElement("format")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public ReportFormat? Format { get; set; }

    /// <summary>Gets or sets the processing status of the request.</summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ReportStatus Status { get; set; }

    /// <summary>Gets or sets the correlation identifier propagated across the request.</summary>
    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the processing time in milliseconds.</summary>
    [BsonElement("durationMs")]
    public double DurationMs { get; set; }

    /// <summary>Gets or sets the number of source records consumed while generating the report.</summary>
    [BsonElement("recordCount")]
    public int RecordCount { get; set; }

    /// <summary>Gets or sets the error message when the generation failed.</summary>
    [BsonElement("errorMessage")]
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the request.</summary>
    [BsonElement("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
