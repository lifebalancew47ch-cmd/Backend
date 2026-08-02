using MongoDB.Driver;
using LifeBalance.Administration.Domain.Entities;

namespace LifeBalance.Administration.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        BsonClassMapRegistrations.Register();

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        CreateIndexes();
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        var collectionName = name ?? CollectionNames.Get<T>();
        return _database.GetCollection<T>(collectionName);
    }

    private void CreateIndexes()
    {
        // Catalogs: unique code + status filter
        var catalogs = GetCollection<Catalog>("catalogs");
        catalogs.Indexes.CreateOne(new CreateIndexModel<Catalog>(
            Builders<Catalog>.IndexKeys.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true }));
        catalogs.Indexes.CreateOne(new CreateIndexModel<Catalog>(
            Builders<Catalog>.IndexKeys.Ascending(x => x.IsActive).Ascending(x => x.Category)));

        // Parameters: unique code + status/category filter
        var parameters = GetCollection<SystemParameter>("parameters");
        parameters.Indexes.CreateOne(new CreateIndexModel<SystemParameter>(
            Builders<SystemParameter>.IndexKeys.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true }));
        parameters.Indexes.CreateOne(new CreateIndexModel<SystemParameter>(
            Builders<SystemParameter>.IndexKeys.Ascending(x => x.IsActive).Ascending(x => x.Category)));

        // Feature flags: unique code
        var flags = GetCollection<FeatureFlag>("feature_flags");
        flags.Indexes.CreateOne(new CreateIndexModel<FeatureFlag>(
            Builders<FeatureFlag>.IndexKeys.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true }));

        // Audit logs: filter + ordering indexes
        var audit = GetCollection<AuditLog>("audit_logs");
        audit.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Descending(x => x.Timestamp)),
            new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.Timestamp)),
            new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Ascending(x => x.Service).Descending(x => x.Timestamp)),
            new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Ascending(x => x.EventType).Descending(x => x.Timestamp)),
            new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Ascending(x => x.CorrelationId))
        });

        // System logs: filter + ordering indexes
        var logs = GetCollection<SystemLog>("system_logs");
        logs.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<SystemLog>(Builders<SystemLog>.IndexKeys.Descending(x => x.Timestamp)),
            new CreateIndexModel<SystemLog>(Builders<SystemLog>.IndexKeys.Ascending(x => x.Service).Descending(x => x.Timestamp)),
            new CreateIndexModel<SystemLog>(Builders<SystemLog>.IndexKeys.Ascending(x => x.Level).Descending(x => x.Timestamp)),
            new CreateIndexModel<SystemLog>(Builders<SystemLog>.IndexKeys.Ascending(x => x.CorrelationId))
        });

        // Service status: one document per service
        var statuses = GetCollection<ServiceStatus>("service_statuses");
        statuses.Indexes.CreateOne(new CreateIndexModel<ServiceStatus>(
            Builders<ServiceStatus>.IndexKeys.Ascending(x => x.Service),
            new CreateIndexOptions { Unique = true }));
    }
}

/// <summary>Mapping between domain entities and Mongo collection names.</summary>
public static class CollectionNames
{
    public static string Get<T>() => Get(typeof(T));

    public static string Get(Type type)
    {
        return type.Name switch
        {
            nameof(Catalog) => "catalogs",
            nameof(SystemParameter) => "parameters",
            nameof(AuditLog) => "audit_logs",
            nameof(SystemLog) => "system_logs",
            nameof(ServiceStatus) => "service_statuses",
            nameof(SystemConfiguration) => "system_configurations",
            nameof(GlobalConfiguration) => "global_configurations",
            nameof(MaintenanceMode) => "maintenance_modes",
            nameof(FeatureFlag) => "feature_flags",
            _ => type.Name.ToLowerInvariant() + "s"
        };
    }
}
