using LifeBalance.OrganizationSaaS.Domain.Common;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class Organization : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty; // RFC / RUT / NIF
    public OrganizationStatus Status { get; private set; } = OrganizationStatus.Active;
    public string PlanId { get; private set; } = string.Empty;
    public string SubscriptionId { get; private set; } = string.Empty;
    public string ConfigurationId { get; private set; } = string.Empty;
    public ContactInfo ContactInfo { get; private set; } = new();
    public Address Address { get; private set; } = new();

    private Organization() { } // For ORM / Serializer

    public Organization(string name, string taxId, string planId, string tenantId, ContactInfo contactInfo, Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        Name = name;
        TaxId = taxId;
        PlanId = planId;
        TenantId = tenantId;
        ContactInfo = contactInfo ?? new ContactInfo();
        Address = address ?? new Address();
        Status = OrganizationStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string taxId, ContactInfo contactInfo, Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        Name = name;
        TaxId = taxId;
        ContactInfo = contactInfo;
        Address = address;
        Touch();
    }

    public void Activate()
    {
        Status = OrganizationStatus.Active;
        Touch();
    }

    public void Suspend()
    {
        Status = OrganizationStatus.Suspended;
        Touch();
    }

    public void Block()
    {
        Status = OrganizationStatus.Blocked;
        Touch();
    }

    public void ChangePlan(string newPlanId)
    {
        PlanId = newPlanId;
        Touch();
    }

    public void LinkSubscription(string subscriptionId)
    {
        SubscriptionId = subscriptionId;
        Touch();
    }

    public void LinkConfiguration(string configurationId)
    {
        ConfigurationId = configurationId;
        Touch();
    }
}
