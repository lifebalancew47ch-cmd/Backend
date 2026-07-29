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

    [BsonElement("organizationId")]
    public string? OrganizationId { get; set; }

    [BsonElement("familyId")]
    public string? FamilyId { get; set; }

    [BsonElement("departmentId")]
    public string? DepartmentId { get; set; }

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

    [BsonElement("isRead")]
    public bool IsRead { get; set; }

    [BsonElement("isArchived")]
    public bool IsArchived { get; set; }

    [BsonElement("isFavorite")]
    public bool IsFavorite { get; set; }

    [BsonElement("deviceTokens")]
    public List<string> DeviceTokens { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("sentAt")]
    public DateTime? SentAt { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("deliveryTimeMs")]
    public long? DeliveryTimeMs { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("provider")]
    public string? Provider { get; set; }
}
