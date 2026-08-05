using MongoDB.Bson.Serialization.Attributes;

namespace LifeBalance.OrganizationSaaS.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract class AggregateRoot : BaseEntity
{
    [BsonIgnore]
    private readonly List<IDomainEvent> _domainEvents = new();

    [BsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
