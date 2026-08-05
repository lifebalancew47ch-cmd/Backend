using LifeBalance.OrganizationSaaS.Domain.Common;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class SaaSPlan : AggregateRoot, IGlobalTenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public PlanTier Tier { get; private set; }
    public decimal PriceMonthly { get; private set; }
    public decimal PriceYearly { get; private set; }
    public string Currency { get; private set; } = "MXN";
    public bool IsCustomPricing { get; private set; }
    public bool IsHighlighted { get; private set; }
    public List<string> Features { get; private set; } = [];
    public PlanLimits Limits { get; private set; } = new();
    public bool IsActive { get; private set; } = true;

    private SaaSPlan() { }

    public SaaSPlan(
        string name,
        PlanTier tier,
        decimal priceMonthly,
        decimal priceYearly,
        PlanLimits limits,
        string currency = "MXN",
        bool isCustomPricing = false,
        bool isHighlighted = false,
        IEnumerable<string>? features = null)
    {
        TenantId = "GLOBAL"; // Global catalog
        Update(name, tier, priceMonthly, priceYearly, limits, currency, isCustomPricing, isHighlighted, features);
        UpdatedAt = null;
        Version = 1;
    }

    public void Update(
        string name,
        PlanTier tier,
        decimal priceMonthly,
        decimal priceYearly,
        PlanLimits limits,
        string currency,
        bool isCustomPricing,
        bool isHighlighted,
        IEnumerable<string>? features)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentNullException.ThrowIfNull(limits);
        if (priceMonthly < 0) throw new ArgumentOutOfRangeException(nameof(priceMonthly));
        if (priceYearly < 0) throw new ArgumentOutOfRangeException(nameof(priceYearly));

        Name = name.Trim();
        Tier = tier;
        PriceMonthly = priceMonthly;
        PriceYearly = priceYearly;
        Currency = currency.Trim().ToUpperInvariant();
        IsCustomPricing = isCustomPricing;
        IsHighlighted = isHighlighted;
        Features = features?.Select(feature => feature.Trim())
            .Where(feature => !string.IsNullOrWhiteSpace(feature))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];
        Limits = limits;
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        Touch();
    }
}
