using System.Reflection;
using MongoDB.Bson.Serialization;
using LifeBalance.Administration.Domain.Entities;

namespace LifeBalance.Administration.Infrastructure.Persistence;

/// <summary>
/// Explicit BSON creator mappings for domain entities whose only public
/// constructors are parameterized. The MongoDB driver cannot auto-map these
/// (it reports "Creator map has N arguments, but none are configured"), so the
/// creator is bound here to the matching properties by name.
/// </summary>
public static class BsonClassMapRegistrations
{
    private static readonly object SyncRoot = new();
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        lock (SyncRoot)
        {
            if (_registered) return;

            RegisterCatalog();
            RegisterSystemParameter();
            RegisterFeatureFlag();
            RegisterServiceStatus();
            RegisterSystemLog();
            RegisterAuditLog();
            RegisterMaintenanceMode();
            RegisterGlobalConfiguration();
            RegisterSystemConfiguration();

            _registered = true;
        }
    }

    private static void RegisterMaintenanceMode()
    {
        BsonClassMap.RegisterClassMap<MaintenanceMode>(cm =>
        {
            cm.AutoMap();
        });
    }

    private static void RegisterGlobalConfiguration()
    {
        BsonClassMap.RegisterClassMap<GlobalConfiguration>(cm =>
        {
            cm.AutoMap();
        });
    }

    private static void RegisterSystemConfiguration()
    {
        BsonClassMap.RegisterClassMap<SystemConfiguration>(cm =>
        {
            cm.AutoMap();
        });
    }

    private static void RegisterCatalog()
    {
        RegisterWithCreator<Catalog>(cm =>
            cm.MapCreator(c => new Catalog(c.Code, c.Name, c.Description, c.Category, c.Items)));
    }

    private static void RegisterSystemParameter()
    {
        RegisterWithCreator<SystemParameter>(cm =>
            cm.MapCreator(p => new SystemParameter(
                p.Code, p.Name, p.Description, p.DataType, p.Value, p.Category,
                p.MinValue, p.MaxValue, p.Unit, p.Order, p.IsSystem)));
    }

    private static void RegisterFeatureFlag()
    {
        RegisterWithCreator<FeatureFlag>(cm =>
            cm.MapCreator(f => new FeatureFlag(f.Code, f.Name, f.Description, f.Category, f.IsSystem)));
    }

    private static void RegisterServiceStatus()
    {
        RegisterWithCreator<ServiceStatus>(cm =>
            cm.MapCreator(s => new ServiceStatus(s.Service, s.ServiceName)));
    }

    private static void RegisterSystemLog()
    {
        RegisterWithCreator<SystemLog>(cm =>
            cm.MapCreator(l => new SystemLog(
                l.Service, l.Level, l.Message, l.Exception, l.StackTrace, l.Source,
                l.UserId, l.CorrelationId, l.Timestamp)));
    }

    private static void RegisterAuditLog()
    {
        RegisterWithCreator<AuditLog>(cm =>
            cm.MapCreator(a => new AuditLog(
                a.UserId, a.UserEmail, a.Action, a.EntityName, a.EntityId,
                a.OperationType, a.EventType, a.Service, a.Endpoint, a.IpAddress,
                a.UserAgent, a.CorrelationId, a.RequestId, a.Result, a.DetailsJson,
                a.OrganizationId, a.CompanyId)));
    }

    /// <summary>
    /// Auto-maps the class and replaces any creator that the conventions might
    /// have registered (the immutable-type convention can create a creator with
    /// no bound arguments for certain constructor signatures) with an explicit
    /// one whose arguments are bound to the class members by name.
    /// </summary>
    private static void RegisterWithCreator<TClass>(Action<BsonClassMap<TClass>> mapCreator)
        where TClass : class
    {
        BsonClassMap.RegisterClassMap<TClass>(cm =>
        {
            cm.AutoMap();
            foreach (var constructorInfo in typeof(TClass).GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                cm.UnmapConstructor(constructorInfo);
            }
            mapCreator(cm);
        });
    }
}
