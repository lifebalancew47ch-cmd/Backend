using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using LifeBalance.Dashboard.Domain.Enums;
using LifeBalance.Dashboard.Domain.ValueObjects;

namespace LifeBalance.Dashboard.Domain.Entities;

/// <summary>
/// Domain entity logging API aggregation metrics and external microservice latencies.
/// </summary>
public class AggregationLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("dashboardType")]
    [BsonRepresentation(BsonType.String)]
    public DashboardType DashboardType { get; set; }

    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public AggregationStatus Status { get; set; }

    [BsonElement("totalDurationMs")]
    public double TotalDurationMs { get; set; }

    [BsonElement("serviceCalls")]
    public List<ServiceCallMetrics> ServiceCalls { get; set; } = new();

    [BsonElement("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
