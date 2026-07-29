using FluentAssertions;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Domain;

public class OrganizationDomainTests
{
    [Fact]
    public void CreateOrganization_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "Tech Corp";
        var taxId = "TAX123456";
        var planId = "PLAN_ENTERPRISE";
        var tenantId = "TENANT_001";
        var contact = new ContactInfo { Email = "admin@techcorp.com" };
        var address = new Address { City = "Mexico City", Country = "Mexico" };

        // Act
        var org = new Organization(name, taxId, planId, tenantId, contact, address);

        // Assert
        org.Should().NotBeNull();
        org.Name.Should().Be(name);
        org.TaxId.Should().Be(taxId);
        org.TenantId.Should().Be(tenantId);
        org.Status.Should().Be(LifeBalance.OrganizationSaaS.Domain.Enums.OrganizationStatus.Active);
        org.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var org = new Organization("Tech Corp", "TAX123", "PLAN_FREE", "TENANT_001", new ContactInfo(), new Address());

        // Act
        org.Suspend();

        // Assert
        org.Status.Should().Be(LifeBalance.OrganizationSaaS.Domain.Enums.OrganizationStatus.Suspended);
    }
}

public class FamilyDomainTests
{
    [Fact]
    public void AddMember_ExceedingLimit_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var family = new Family("Gomez Family", "USER_ADMIN", "TENANT_001", maxMembers: 2);

        // Act
        family.AddMember("USER_002");
        Action act = () => family.AddMember("USER_003");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Family member limit of 2 reached*");
    }
}
