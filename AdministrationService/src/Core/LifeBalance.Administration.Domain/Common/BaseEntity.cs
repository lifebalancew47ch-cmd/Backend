using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.Administration.Domain.Common;

/// <summary>
/// Base class for every domain entity persisted by the Administration Service.
/// This is a global administration service (no multi-tenancy), therefore the
/// base contract is intentionally smaller than tenant-scoped services.
/// </summary>
public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; protected set; } = ObjectId.GenerateNewId().ToString();

    public bool IsDeleted { get; protected set; }

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
}
