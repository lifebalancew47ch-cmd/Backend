namespace LifeBalance.Reporting.Infrastructure.Persistence.Mongo;

/// <summary>
/// Strongly-typed configuration options for MongoDB.
/// Bound from <c>appsettings.json → MongoDb</c> section.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "MongoDb";

    /// <summary>Gets or sets the MongoDB connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the target database name.</summary>
    public string DatabaseName { get; set; } = Domain.Constants.DomainConstants.DatabaseName;
}
