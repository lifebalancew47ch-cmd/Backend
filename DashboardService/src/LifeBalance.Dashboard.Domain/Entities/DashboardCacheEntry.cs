using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using LifeBalance.Dashboard.Domain.Enums;

namespace LifeBalance.Dashboard.Domain.Entities;

/// <summary>
/// Represents a cached aggregate view stored in MongoDB for high performance retrieval.
/// </summary>
public class DashboardCacheEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("cacheKey")]
    public string CacheKey { get; set; } = string.Empty;

    [BsonElement("dashboardType")]
    [BsonRepresentation(BsonType.String)]
    public DashboardType DashboardType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("payloadJson")]
    public string PayloadJson { get; set; } = string.Empty;

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("isExpired")]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
