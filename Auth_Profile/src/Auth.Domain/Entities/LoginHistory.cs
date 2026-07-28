using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Entities;

public class LoginHistory : BaseEntity
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("device")]
    public string? Device { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("success")]
    public bool Success { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("loginAt")]
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
}
