using LifeBalance.Notifications.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class MetricsRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [BsonElement("totalSent")]
    public long TotalSent { get; set; }

    [BsonElement("delivered")]
    public long Delivered { get; set; }

    [BsonElement("failed")]
    public long Failed { get; set; }

    [BsonElement("pending")]
    public long Pending { get; set; }

    [BsonElement("opened")]
    public long Opened { get; set; }

    [BsonElement("read")]
    public long Read { get; set; }

    [BsonElement("channelBreakdown")]
    public Dictionary<NotificationChannel, long> ChannelBreakdown { get; set; } = new();

    [BsonElement("averageDeliveryTimeMs")]
    public double AverageDeliveryTimeMs { get; set; }

    [BsonElement("errorCount")]
    public long ErrorCount { get; set; }
}
