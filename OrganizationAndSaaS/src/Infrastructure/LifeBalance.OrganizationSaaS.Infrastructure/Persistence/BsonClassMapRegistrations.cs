using System.Reflection;
using MongoDB.Bson.Serialization;
using LifeBalance.OrganizationSaaS.Domain.Entities;

namespace LifeBalance.OrganizationSaaS.Infrastructure.Persistence;

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

            RegisterOrganization();
            RegisterFamily();
            RegisterDepartment();
            RegisterTeam();
            RegisterLicense();
            RegisterMembership();
            RegisterSubscription();
            RegisterInvitation();
            RegisterSaaSPlan();

            _registered = true;
        }
    }

    private static void RegisterOrganization()
    {
        RegisterWithCreator<Organization>(cm =>
            cm.MapCreator(o => new Organization(o.Name, o.TaxId, o.PlanId, o.TenantId, o.ContactInfo, o.Address)));
    }

    private static void RegisterFamily()
    {
        RegisterWithCreator<Family>(cm =>
            cm.MapCreator(f => new Family(f.Name, f.AdministratorUserId, f.TenantId, f.MaxMembers)));
    }

    private static void RegisterDepartment()
    {
        RegisterWithCreator<Department>(cm =>
            cm.MapCreator(d => new Department(d.OrganizationId, d.Name, d.Description, d.TenantId, d.ManagerUserId, d.ParentDepartmentId)));
    }

    private static void RegisterTeam()
    {
        RegisterWithCreator<Team>(cm =>
            cm.MapCreator(t => new Team(t.OrganizationId, t.Name, t.TenantId, t.DepartmentId, t.LeaderUserId)));
    }

    private static void RegisterLicense()
    {
        RegisterWithCreator<License>(cm =>
            cm.MapCreator(l => new License(l.OrganizationId, l.Type, l.ExpiresAt, l.TenantId)));
    }

    private static void RegisterMembership()
    {
        RegisterWithCreator<Membership>(cm =>
            cm.MapCreator(m => new Membership(m.UserId, m.PlanId, m.TenantId, m.OrganizationId, m.FamilyId, m.Role)));
    }

    private static void RegisterSubscription()
    {
        RegisterWithCreator<Subscription>(cm =>
            cm.MapCreator(s => new Subscription(s.OrganizationId, s.PlanId, s.BillingCycle, s.TenantId)));
    }

    private static void RegisterInvitation()
    {
        RegisterWithCreator<Invitation>(cm =>
            cm.MapCreator(i => new Invitation(i.TargetEmail, i.TenantId, i.OrganizationId, i.FamilyId, i.Role)));
    }

    private static void RegisterSaaSPlan()
    {
        RegisterWithCreator<SaaSPlan>(cm =>
            cm.MapCreator(p => new SaaSPlan(p.Name, p.Tier, p.PriceMonthly, p.PriceYearly, p.Limits, p.Currency, p.IsCustomPricing, p.IsHighlighted, p.Features)));
    }

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
