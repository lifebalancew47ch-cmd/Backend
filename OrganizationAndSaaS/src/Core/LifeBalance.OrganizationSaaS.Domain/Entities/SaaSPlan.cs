using LifeBalance.OrganizationSaaS.Domain.Common;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class SaaSPlan : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public PlanTier Tier { get; private set; }
    public decimal PriceMonthly { get; private set; }
    public decimal PriceYearly { get; private set; }
    public PlanLimits Limits { get; private set; } = new();

    private SaaSPlan() { }

    public SaaSPlan(string name, PlanTier tier, decimal priceMonthly, decimal priceYearly, PlanLimits limits)
    {
        Name = name;
        Tier = tier;
        PriceMonthly = priceMonthly;
        PriceYearly = priceYearly;
        Limits = limits;
        TenantId = "GLOBAL"; // Global catalog
    }
}
