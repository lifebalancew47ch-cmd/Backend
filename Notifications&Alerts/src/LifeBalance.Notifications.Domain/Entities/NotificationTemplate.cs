using LifeBalance.Notifications.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class NotificationTemplate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("subject")]
    public string Subject { get; set; } = string.Empty;

    [BsonElement("bodyContent")]
    public string BodyContent { get; set; } = string.Empty;

    [BsonElement("htmlContent")]
    public string? HtmlContent { get; set; }

    [BsonElement("type")]
    public NotificationType Type { get; set; }

    [BsonElement("channel")]
    public NotificationChannel Channel { get; set; }

    [BsonElement("variables")]
    public List<string> Variables { get; set; } = new();

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isGlobal")]
    public bool IsGlobal { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
