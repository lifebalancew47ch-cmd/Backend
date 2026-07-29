using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Notifications.Domain.Entities;

public class NotificationPreference
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("receivePush")]
    public bool ReceivePush { get; set; } = true;

    [BsonElement("receiveWearOS")]
    public bool ReceiveWearOS { get; set; } = true;

    [BsonElement("receiveEmail")]
    public bool ReceiveEmail { get; set; } = true;

    [BsonElement("receiveSedentaryAlerts")]
    public bool ReceiveSedentaryAlerts { get; set; } = true;

    [BsonElement("receiveMarketing")]
    public bool ReceiveMarketing { get; set; } = true;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
