using MongoDB.Driver;
using LifeBalance.OrganizationSaaS.Domain.Entities;

namespace LifeBalance.OrganizationSaaS.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        CreateIndexes();
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        var collectionName = name ?? typeof(T).Name.ToLowerPlural();
        return _database.GetCollection<T>(collectionName);
    }

    private void CreateIndexes()
    {
        // Organizations index
        var orgs = GetCollection<Organization>("organizations");
        orgs.Indexes.CreateOne(new CreateIndexModel<Organization>(
            Builders<Organization>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.IsDeleted)
        ));

        // Families index
        var families = GetCollection<Family>("families");
        families.Indexes.CreateOne(new CreateIndexModel<Family>(
            Builders<Family>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.IsDeleted)
        ));

        // Departments index
        var depts = GetCollection<Department>("departments");
        depts.Indexes.CreateOne(new CreateIndexModel<Department>(
            Builders<Department>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.OrganizationId)
        ));

        // Teams index
        var teams = GetCollection<Team>("teams");
        teams.Indexes.CreateOne(new CreateIndexModel<Team>(
            Builders<Team>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.OrganizationId)
        ));

        // Licenses index
        var licenses = GetCollection<License>("licenses");
        licenses.Indexes.CreateOne(new CreateIndexModel<License>(
            Builders<License>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.LicenseKey),
            new CreateIndexOptions { Unique = true }
        ));

        // Invitations index
        var invs = GetCollection<Invitation>("invitations");
        invs.Indexes.CreateOne(new CreateIndexModel<Invitation>(
            Builders<Invitation>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Token),
            new CreateIndexOptions { Unique = true }
        ));
    }
}

public static class StringExtensions
{
    public static string ToLowerPlural(this string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            return name[..^1].ToLowerInvariant() + "ies";
        return name.ToLowerInvariant() + "s";
    }
}
