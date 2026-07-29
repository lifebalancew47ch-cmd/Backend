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

    [BsonElement("receiveEmail")]
    public bool ReceiveEmail { get; set; } = true;

    [BsonElement("receiveSms")]
    public bool ReceiveSms { get; set; }

    [BsonElement("receiveWearOS")]
    public bool ReceiveWearOS { get; set; } = true;

    [BsonElement("receiveCriticalAlerts")]
    public bool ReceiveCriticalAlerts { get; set; } = true;

    [BsonElement("receiveReminders")]
    public bool ReceiveReminders { get; set; } = true;

    [BsonElement("receiveGoals")]
    public bool ReceiveGoals { get; set; } = true;

    [BsonElement("receiveGamification")]
    public bool ReceiveGamification { get; set; } = true;

    [BsonElement("receiveOrganizational")]
    public bool ReceiveOrganizational { get; set; } = true;

    [BsonElement("allowedStartTime")]
    public TimeSpan? AllowedStartTime { get; set; }

    [BsonElement("allowedEndTime")]
    public TimeSpan? AllowedEndTime { get; set; }

    [BsonElement("quietModeEnabled")]
    public bool QuietModeEnabled { get; set; }

    [BsonElement("quietModeStart")]
    public TimeSpan? QuietModeStart { get; set; }

    [BsonElement("quietModeEnd")]
    public TimeSpan? QuietModeEnd { get; set; }

    [BsonElement("frequency")]
    public string? Frequency { get; set; }

    [BsonElement("language")]
    public string Language { get; set; } = "en";

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
