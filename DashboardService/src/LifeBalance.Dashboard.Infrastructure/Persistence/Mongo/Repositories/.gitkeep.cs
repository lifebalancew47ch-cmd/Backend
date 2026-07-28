namespace LifeBalance.Dashboard.Infrastructure.Persistence.Mongo.Repositories;

// Placeholder for MongoDB repository implementations.
//
// Each repository should:
//  1. Implement an IRepository<TAggregate, TId> interface from Domain
//  2. Inject MongoDbContext and work with IMongoCollection<TDocument>
//  3. Be registered in Infrastructure/DependencyInjection.cs
//
// Example:
// public sealed class DashboardSnapshotRepository : IRepository<DashboardSnapshot, Guid>
// {
//     private readonly IMongoCollection<DashboardSnapshotDocument> _collection;
//     public DashboardSnapshotRepository(MongoDbContext context)
//         => _collection = context.GetCollection<DashboardSnapshotDocument>("dashboard_snapshots");
// }
