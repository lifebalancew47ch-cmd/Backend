using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Entities;

public class User : BaseEntity
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isEmailConfirmed")]
    public bool IsEmailConfirmed { get; set; } = false;

    [BsonElement("isPhoneConfirmed")]
    public bool IsPhoneConfirmed { get; set; } = false;

    [BsonElement("isTwoFactorEnabled")]
    public bool IsTwoFactorEnabled { get; set; } = false;

    [BsonElement("twoFactorSecret")]
    public string? TwoFactorSecret { get; set; }

    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    [BsonElement("lockoutEnd")]
    public DateTime? LockoutEnd { get; set; }

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("lastPasswordChangeAt")]
    public DateTime? LastPasswordChangeAt { get; set; }

    [BsonElement("roleIds")]
    public List<string> RoleIds { get; set; } = new();

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        MarkUpdated();
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        MarkUpdated();
    }

    public void LockOut(TimeSpan duration)
    {
        LockoutEnd = DateTime.UtcNow.Add(duration);
        MarkUpdated();
    }
}
