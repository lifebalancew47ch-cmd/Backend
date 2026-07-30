using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.OrganizationSaaS.Domain.Common;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; protected set; } = ObjectId.GenerateNewId().ToString();

    public string TenantId { get; protected set; } = string.Empty;

    public bool IsDeleted { get; protected set; } = false;

    public DateTime? DeletedAt { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; protected set; }

    [BsonElement("version")]
    public int Version { get; protected set; } = 1;

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        Touch();
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void SetTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        TenantId = tenantId;
    }
}
