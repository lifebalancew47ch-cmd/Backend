using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Entities;

public class RefreshToken : BaseEntity
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("jwtId")]
    public string JwtId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("createdByIp")]
    public string CreatedByIp { get; set; } = string.Empty;

    [BsonElement("revokedByIp")]
    public string? RevokedByIp { get; set; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    [BsonElement("replacedByToken")]
    public string? ReplacedByToken { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActiveAndNotExpired => IsActive && !IsExpired && !IsRevoked;
}
