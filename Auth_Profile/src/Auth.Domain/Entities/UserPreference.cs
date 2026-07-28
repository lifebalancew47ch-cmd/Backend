using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Entities;

public class UserPreference : BaseEntity
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("theme")]
    public string Theme { get; set; } = "light";

    [BsonElement("language")]
    public string Language { get; set; } = "en";

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("unitsSystem")]
    public string UnitsSystem { get; set; } = "metric";

    [BsonElement("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = true;

    [BsonElement("emailNotificationsEnabled")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    [BsonElement("pushNotificationsEnabled")]
    public bool PushNotificationsEnabled { get; set; } = true;

    [BsonElement("profileVisibility")]
    public string ProfileVisibility { get; set; } = "public";

    [BsonElement("marketingConsent")]
    public bool MarketingConsent { get; set; } = false;

    [BsonElement("activitySharing")]
    public bool ActivitySharing { get; set; } = true;
}
