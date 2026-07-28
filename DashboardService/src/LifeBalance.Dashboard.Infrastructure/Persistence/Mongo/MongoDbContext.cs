using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace LifeBalance.Dashboard.Infrastructure.Persistence.Mongo;

/// <summary>
/// Provides access to the MongoDB database instance.
/// Registered as a singleton so the <see cref="MongoClient"/> connection pool is reused.
/// </summary>
public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoDbContext"/>.
    /// </summary>
    /// <param name="options">The MongoDB configuration options.</param>
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    /// <summary>
    /// Gets a strongly-typed collection from the database.
    /// </summary>
    /// <typeparam name="TDocument">The BSON document type.</typeparam>
    /// <param name="collectionName">The name of the collection.</param>
    /// <returns>An <see cref="IMongoCollection{TDocument}"/> instance.</returns>
    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
        => _database.GetCollection<TDocument>(collectionName);

    /// <summary>Gets the underlying <see cref="IMongoDatabase"/> for advanced operations.</summary>
    public IMongoDatabase Database => _database;
}
