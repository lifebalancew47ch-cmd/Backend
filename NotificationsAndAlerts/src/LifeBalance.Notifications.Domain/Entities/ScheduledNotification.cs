using LifeBalance.Notifications.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class ScheduledNotification
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

    [BsonElement("scheduledFor")]
    public DateTime ScheduledFor { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
