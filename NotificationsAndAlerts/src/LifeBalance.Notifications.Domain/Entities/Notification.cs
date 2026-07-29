using LifeBalance.Notifications.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("body")]
    public string Body { get; set; } = string.Empty;

    [BsonElement("payload")]
    public string? Payload { get; set; }

    [BsonElement("type")]
    public NotificationType Type { get; set; }

    [BsonElement("channel")]
    public NotificationChannel Channel { get; set; }

    [BsonElement("status")]
    public NotificationStatus Status { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("sentAt")]
    public DateTime? SentAt { get; set; }
}
