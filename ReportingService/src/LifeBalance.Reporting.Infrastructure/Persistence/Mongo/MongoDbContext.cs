using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LifeBalance.Reporting.Infrastructure.Persistence.Mongo;

/// <summary>
/// Provides access to the MongoDB database instance.
/// Registered as a singleton so the <see cref="MongoClient"/> connection pool is reused.
/// </summary>
public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>Initializes a new instance of <see cref="MongoDbContext"/>.</summary>
    /// <param name="options">The MongoDB configuration options.</param>
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);
        Client = client;
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    /// <summary>
    /// Ensures the required indexes exist. Runs in the background at startup so index
    /// creation never blocks request processing or the health checks.
    /// </summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
        => Indexes.EnsureIndexesAsync(_database, cancellationToken);

    /// <summary>Gets the underlying <see cref="IMongoClient"/> for advanced operations.</summary>
    public IMongoClient Client { get; }

    /// <summary>
    /// Gets a strongly-typed collection from the database.
    /// </summary>
    /// <typeparam name="TDocument">The BSON document type.</typeparam>
    /// <param name="collectionName">The name of the collection.</param>
    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
        => _database.GetCollection<TDocument>(collectionName);

    /// <summary>Gets the underlying <see cref="IMongoDatabase"/> for advanced operations.</summary>
    public IMongoDatabase Database => _database;
}
