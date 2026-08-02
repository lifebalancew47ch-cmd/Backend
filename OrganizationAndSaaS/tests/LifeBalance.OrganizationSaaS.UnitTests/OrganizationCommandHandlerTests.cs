using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.Organizations;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public partial class OrganizationCommandHandlerTests
{
    private static Organization CreateOrg(string id = "ORG_1", string name = "Initech Corp")
        => new(name, "TAX999", "PLAN_BUSINESS", "TENANT_TEST", new ContactInfo { Email = "a@b.com" }, new Address { City = "CDMX" });

    [Fact]
    public async Task Handle_CreateOrganization_WithMissingTenantId_ShouldGenerateTenantId()
    {
        _mockTenantContext.Setup(x => x.TenantId).Returns("  ");
        var command = new CreateOrganizationCommand("Initech Corp", "TAX999", "PLAN_BUSINESS", new ContactInfo(), new Address());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.TenantId.Should().NotBeNullOrWhiteSpace();
        result.Data!.TenantId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task Handle_CreateOrganization_WithTenantFromContext_ShouldPersistTenant()
    {
        var command = new CreateOrganizationCommand("Initech Corp", "TAX999", "PLAN_BUSINESS", new ContactInfo(), new Address());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_CreateOrganization_ShouldReturnDtoWithActiveStatusAndSuccessMessage()
    {
        var command = new CreateOrganizationCommand("Initech Corp", "TAX999", "PLAN_BUSINESS", new ContactInfo { Email = "a@b.com" }, new Address { City = "CDMX" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization created successfully.");
        result.Data!.Status.Should().Be(OrganizationStatus.Active.ToString());
        result.Data!.TaxId.Should().Be("TAX999");
        result.Data!.PlanId.Should().Be("PLAN_BUSINESS");
        result.Data!.ContactInfo.Email.Should().Be("a@b.com");
        result.Data!.Address.City.Should().Be("CDMX");
    }

    [Fact]
    public async Task Handle_CreateOrganization_WithNullContactAndAddress_ShouldNotThrow()
    {
        var command = new CreateOrganizationCommand("Initech Corp", "TAX999", "PLAN_BUSINESS", null!, null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.ContactInfo.Should().NotBeNull();
        result.Data!.Address.Should().NotBeNull();
        result.Data!.ContactInfo.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UpdateOrganization_ShouldReturnUpdatedDto()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        var command = new UpdateOrganizationCommand("ORG_1", "Renamed Corp", "TAX888", new ContactInfo { Email = "new@x.com" }, new Address { City = "Madrid" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization updated successfully.");
        result.Data!.Name.Should().Be("Renamed Corp");
        result.Data!.TaxId.Should().Be("TAX888");
        result.Data!.ContactInfo.Email.Should().Be("new@x.com");
        result.Data!.Address.City.Should().Be("Madrid");
    }

    [Fact]
    public async Task Handle_UpdateOrganization_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new UpdateOrganizationCommand("ORG_1", "New Name", "TAX888", new ContactInfo(), new Address()), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_UpdateOrganization_ShouldPersistModifiedEntity()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        Organization? updated = null;
        _mockOrgRepo.Setup(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((o, _) => updated = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateOrganizationCommand("ORG_1", "Renamed", "TAX777", new ContactInfo(), new Address()), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Renamed");
        updated.UpdatedAt.Should().NotBeNull();
        updated.Version.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UpdateOrganization_ShouldLookupAndUpdateExactlyOnce()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        await _handler.Handle(new UpdateOrganizationCommand("ORG_1", "Renamed", "TAX777", new ContactInfo(), new Address()), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockOrgRepo.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateOrganization_ShouldPreserveStatusAndTenant()
    {
        var org = CreateOrg();
        org.Suspend();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new UpdateOrganizationCommand("ORG_1", "Renamed", "TAX777", new ContactInfo(), new Address()), CancellationToken.None);

        result.Data!.Status.Should().Be(OrganizationStatus.Suspended.ToString());
        result.Data!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_ActivateOrganization_ShouldReturnTrueAndSetActiveStatus()
    {
        var org = CreateOrg();
        org.Suspend();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new ActivateOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization activated.");
        result.Data.Should().BeTrue();
        org.Status.Should().Be(OrganizationStatus.Active);
    }

    [Fact]
    public async Task Handle_ActivateOrganization_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new ActivateOrganizationCommand("ORG_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_ActivateOrganization_ShouldPersistModifiedEntity()
    {
        var org = CreateOrg();
        org.Suspend();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        await _handler.Handle(new ActivateOrganizationCommand("ORG_1"), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.UpdateAsync(org, It.IsAny<CancellationToken>()), Times.Once);
        org.Status.Should().Be(OrganizationStatus.Active);
    }

    [Fact]
    public async Task Handle_ActivateOrganization_OnActiveOrganization_ShouldBeIdempotent()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new ActivateOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Data.Should().BeTrue();
        org.Status.Should().Be(OrganizationStatus.Active);
        _mockOrgRepo.Verify(x => x.UpdateAsync(org, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActivateOrganization_ShouldPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var org = CreateOrg();
        org.Suspend();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", cts.Token)).ReturnsAsync(org);

        var result = await _handler.Handle(new ActivateOrganizationCommand("ORG_1"), cts.Token);

        result.Data.Should().BeTrue();
        _mockOrgRepo.Verify(x => x.UpdateAsync(org, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_SuspendOrganization_ShouldReturnTrueAndSetSuspendedStatus()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new SuspendOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization suspended.");
        result.Data.Should().BeTrue();
        org.Status.Should().Be(OrganizationStatus.Suspended);
    }

    [Fact]
    public async Task Handle_SuspendOrganization_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new SuspendOrganizationCommand("ORG_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_SuspendOrganization_OnSuspendedOrganization_ShouldBeIdempotent()
    {
        var org = CreateOrg();
        org.Suspend();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new SuspendOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Data.Should().BeTrue();
        org.Status.Should().Be(OrganizationStatus.Suspended);
    }

    [Fact]
    public async Task Handle_SuspendOrganization_ShouldPersistModifiedEntity()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        await _handler.Handle(new SuspendOrganizationCommand("ORG_1"), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.UpdateAsync(org, It.IsAny<CancellationToken>()), Times.Once);
        org.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SuspendOrganization_ShouldPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", cts.Token)).ReturnsAsync(org);

        var result = await _handler.Handle(new SuspendOrganizationCommand("ORG_1"), cts.Token);

        result.Data.Should().BeTrue();
        _mockOrgRepo.Verify(x => x.UpdateAsync(org, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_RestoreOrganization_ShouldReturnTrueAndClearSoftDelete()
    {
        var org = CreateOrg();
        org.SoftDelete();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new RestoreOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization restored.");
        result.Data.Should().BeTrue();
        org.IsDeleted.Should().BeFalse();
        org.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RestoreOrganization_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new RestoreOrganizationCommand("ORG_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_RestoreOrganization_OnNonDeletedOrganization_ShouldBeIdempotent()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new RestoreOrganizationCommand("ORG_1"), CancellationToken.None);

        result.Data.Should().BeTrue();
        org.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RestoreOrganization_ShouldPersistModifiedEntity()
    {
        var org = CreateOrg();
        org.SoftDelete();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        await _handler.Handle(new RestoreOrganizationCommand("ORG_1"), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.UpdateAsync(org, It.IsAny<CancellationToken>()), Times.Once);
        org.Version.Should().Be(3);
    }

    [Fact]
    public async Task Handle_RestoreOrganization_ShouldLookupAndUpdateExactlyOnce()
    {
        var org = CreateOrg();
        org.SoftDelete();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        await _handler.Handle(new RestoreOrganizationCommand("ORG_1"), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockOrgRepo.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_ShouldReturnTrueAndUpdatePlan()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _mockPlanRepo.Setup(x => x.GetByIdAsync("PLAN_ENTERPRISE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaaSPlan("Enterprise", PlanTier.Enterprise, 199m, 1990m, PlanLimits.DefaultEnterprise()));

        var result = await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Organization plan updated.");
        result.Data.Should().BeTrue();
        org.PlanId.Should().Be("PLAN_ENTERPRISE");
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_WhenPlanNotFound_ShouldThrowResourceNotFoundException()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _mockPlanRepo.Setup(x => x.GetByIdAsync("PLAN_UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((SaaSPlan?)null);

        var act = async () => await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*SaaSPlan*PLAN_UNKNOWN*");
        _mockOrgRepo.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_ShouldPersistModifiedEntity()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _mockPlanRepo.Setup(x => x.GetByIdAsync("PLAN_ENTERPRISE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaaSPlan("Enterprise", PlanTier.Enterprise, 199m, 1990m, PlanLimits.DefaultEnterprise()));
        Organization? updated = null;
        _mockOrgRepo.Setup(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((o, _) => updated = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.PlanId.Should().Be("PLAN_ENTERPRISE");
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_ToSamePlan_ShouldBeIdempotent()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _mockPlanRepo.Setup(x => x.GetByIdAsync("PLAN_BUSINESS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaaSPlan("Business", PlanTier.Business, 49m, 490m, PlanLimits.DefaultFree()));

        var result = await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_BUSINESS"), CancellationToken.None);

        result.Data.Should().BeTrue();
        org.PlanId.Should().Be("PLAN_BUSINESS");
    }

    [Fact]
    public async Task Handle_ChangeOrganizationPlan_ShouldLookupAndUpdateExactlyOnce()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _mockPlanRepo.Setup(x => x.GetByIdAsync("PLAN_ENTERPRISE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaaSPlan("Enterprise", PlanTier.Enterprise, 199m, 1990m, PlanLimits.DefaultEnterprise()));

        await _handler.Handle(new ChangeOrganizationPlanCommand("ORG_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        _mockOrgRepo.Verify(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockPlanRepo.Verify(x => x.GetByIdAsync("PLAN_ENTERPRISE", It.IsAny<CancellationToken>()), Times.Once);
        _mockOrgRepo.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
