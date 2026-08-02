using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Infrastructure.Persistence;
using MongoDB.Bson.Serialization;
using Xunit;

namespace LifeBalance.Administration.IntegrationTests;

public class BsonClassMapRegistrationTests
{
    [Fact]
    public void All_persisted_entities_have_a_resolvable_bson_serializer()
    {
        BsonClassMapRegistrations.Register();

        var types = new[]
        {
            typeof(Catalog), typeof(CatalogItem), typeof(SystemParameter),
            typeof(FeatureFlag), typeof(ServiceStatus), typeof(SystemLog),
            typeof(AuditLog), typeof(MaintenanceMode), typeof(SystemConfiguration),
            typeof(GlobalConfiguration)
        };

        foreach (var t in types)
        {
            var serializer = BsonSerializer.LookupSerializer(t);
            Assert.NotNull(serializer);
        }
    }
}
