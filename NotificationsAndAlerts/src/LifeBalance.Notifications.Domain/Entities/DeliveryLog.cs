using LifeBalance.Notifications.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class DeliveryLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("notificationId")]
    public string NotificationId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("channel")]
    public NotificationChannel Channel { get; set; }

    [BsonElement("status")]
    public NotificationStatus Status { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; } = 1;

    [BsonElement("deliveryTimeMs")]
    public long? DeliveryTimeMs { get; set; }

    [BsonElement("openedAt")]
    public DateTime? OpenedAt { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("provider")]
    public string? Provider { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
