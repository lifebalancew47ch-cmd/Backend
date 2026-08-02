using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.LicensesAndSubscriptions;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using License = LifeBalance.OrganizationSaaS.Domain.Entities.License;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class LicenseAndSubscriptionCommandHandlerTests
{
    private readonly Mock<IRepository<License>> _mockLicenseRepo;
    private readonly Mock<IRepository<Subscription>> _mockSubscriptionRepo;
    private readonly Mock<IRepository<Invitation>> _mockInvitationRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<INotificationServiceClient> _mockNotificationClient;
    private readonly LicenseAndSubscriptionCommandHandler _handler;

    public LicenseAndSubscriptionCommandHandlerTests()
    {
        _mockLicenseRepo = new Mock<IRepository<License>>();
        _mockSubscriptionRepo = new Mock<IRepository<Subscription>>();
        _mockInvitationRepo = new Mock<IRepository<Invitation>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockNotificationClient = new Mock<INotificationServiceClient>();

        _mockTenantContext.Setup(x => x.TenantId).Returns("TENANT_TEST");

        _handler = new LicenseAndSubscriptionCommandHandler(
            _mockLicenseRepo.Object,
            _mockSubscriptionRepo.Object,
            _mockInvitationRepo.Object,
            _mockTenantContext.Object,
            _mockNotificationClient.Object);
    }

    private static License CreateLicense(string id = "LIC_1", string type = "Standard", string? assignedUser = null)
    {
        var license = new License("ORG_1", type, DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        if (assignedUser != null)
        {
            license.AssignToUser(assignedUser);
        }
        return license;
    }

    [Fact]
    public async Task Handle_CreateLicense_ShouldReturnMappedDtoAndSuccessMessage()
    {
        var result = await _handler.Handle(new CreateLicenseCommand("ORG_1", "Standard", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("License issued successfully.");
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.Type.Should().Be("Standard");
        result.Data!.Status.Should().Be(LicenseStatus.Available.ToString());
        result.Data!.LicenseKey.Should().NotBeNullOrWhiteSpace();
        result.Data!.LicenseKey.Should().MatchRegex("^[0-9A-F]{32}$");
        result.Data!.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CreateLicense_ShouldPropagateTenantIdFromContext()
    {
        License? created = null;
        _mockLicenseRepo.Setup(x => x.AddAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()))
            .Callback<License, CancellationToken>((l, _) => created = l)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateLicenseCommand("ORG_1", "Standard", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        created.Should().NotBeNull();
        created!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_CreateLicense_WithMissingTenantId_ShouldGenerateTenantId()
    {
        _mockTenantContext.Setup(x => x.TenantId).Returns(string.Empty);
        License? created = null;
        _mockLicenseRepo.Setup(x => x.AddAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()))
            .Callback<License, CancellationToken>((l, _) => created = l)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateLicenseCommand("ORG_1", "Standard", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        created!.TenantId.Should().NotBeNullOrWhiteSpace();
        created!.TenantId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task Handle_CreateLicense_ShouldPersistExpirationAndType()
    {
        var expiresAt = DateTime.UtcNow.AddMonths(6);
        License? created = null;
        _mockLicenseRepo.Setup(x => x.AddAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()))
            .Callback<License, CancellationToken>((l, _) => created = l)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateLicenseCommand("ORG_1", "Enterprise", expiresAt), CancellationToken.None);

        created!.Type.Should().Be("Enterprise");
        created!.ExpiresAt.Should().Be(expiresAt);
        created!.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_CreateLicense_ShouldAddOnceAndPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new CreateLicenseCommand("ORG_1", "Pro", DateTime.UtcNow.AddYears(1)), cts.Token);

        _mockLicenseRepo.Verify(x => x.AddAsync(It.IsAny<License>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_AssignLicense_ShouldReturnTrueAndAssignUser()
    {
        var license = CreateLicense();
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var result = await _handler.Handle(new AssignLicenseCommand("LIC_1", "USER_5"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("License assigned to user.");
        result.Data.Should().BeTrue();
        license.Status.Should().Be(LicenseStatus.Assigned);
        license.AssignedUserId.Should().Be("USER_5");
    }

    [Fact]
    public async Task Handle_AssignLicense_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync((License?)null);

        var act = async () => await _handler.Handle(new AssignLicenseCommand("LIC_1", "USER_5"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*License*LIC_1*");
    }

    [Fact]
    public async Task Handle_AssignLicense_WhenAlreadyAssigned_ShouldThrowInvalidOperationException()
    {
        var license = CreateLicense(assignedUser: "USER_1");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var act = async () => await _handler.Handle(new AssignLicenseCommand("LIC_1", "USER_5"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("License is not available for assignment.");
    }

    [Fact]
    public async Task Handle_AssignLicense_ShouldPersistModifiedEntity()
    {
        var license = CreateLicense();
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        await _handler.Handle(new AssignLicenseCommand("LIC_1", "USER_5"), CancellationToken.None);

        _mockLicenseRepo.Verify(x => x.UpdateAsync(license, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AssignLicense_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateLicense());

        await _handler.Handle(new AssignLicenseCommand("LIC_1", "USER_5"), CancellationToken.None);

        _mockLicenseRepo.Verify(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockLicenseRepo.Verify(x => x.UpdateAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RevokeLicense_ShouldReturnTrueAndSetRevokedStatus()
    {
        var license = CreateLicense(assignedUser: "USER_5");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var result = await _handler.Handle(new RevokeLicenseCommand("LIC_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("License revoked.");
        result.Data.Should().BeTrue();
        license.Status.Should().Be(LicenseStatus.Revoked);
        license.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RevokeLicense_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync((License?)null);

        var act = async () => await _handler.Handle(new RevokeLicenseCommand("LIC_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*License*LIC_1*");
    }

    [Fact]
    public async Task Handle_RevokeLicense_OnAvailableLicense_ShouldBeIdempotent()
    {
        var license = CreateLicense();
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var result = await _handler.Handle(new RevokeLicenseCommand("LIC_1"), CancellationToken.None);

        result.Data.Should().BeTrue();
        license.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task Handle_RevokeLicense_ShouldPersistModifiedEntity()
    {
        var license = CreateLicense(assignedUser: "USER_5");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        await _handler.Handle(new RevokeLicenseCommand("LIC_1"), CancellationToken.None);

        _mockLicenseRepo.Verify(x => x.UpdateAsync(license, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RevokeLicense_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateLicense(assignedUser: "USER_5"));

        await _handler.Handle(new RevokeLicenseCommand("LIC_1"), CancellationToken.None);

        _mockLicenseRepo.Verify(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockLicenseRepo.Verify(x => x.UpdateAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RenewLicense_ShouldReturnTrueAndUpdateExpiration()
    {
        var license = CreateLicense();
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);
        var newExpiration = DateTime.UtcNow.AddYears(2);

        var result = await _handler.Handle(new RenewLicenseCommand("LIC_1", newExpiration), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("License renewed.");
        result.Data.Should().BeTrue();
        license.ExpiresAt.Should().Be(newExpiration);
    }

    [Fact]
    public async Task Handle_RenewLicense_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync((License?)null);

        var act = async () => await _handler.Handle(new RenewLicenseCommand("LIC_1", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*License*LIC_1*");
    }

    [Fact]
    public async Task Handle_RenewLicense_OnExpiredLicenseWithoutUser_ShouldBecomeAvailable()
    {
        var license = CreateLicense();
        typeof(License).GetProperty(nameof(License.Status))!.SetValue(license, LicenseStatus.Expired);
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        await _handler.Handle(new RenewLicenseCommand("LIC_1", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        license.Status.Should().Be(LicenseStatus.Available);
    }

    [Fact]
    public async Task Handle_RenewLicense_OnExpiredLicenseWithUser_ShouldBecomeAssigned()
    {
        var license = CreateLicense(assignedUser: "USER_5");
        typeof(License).GetProperty(nameof(License.Status))!.SetValue(license, LicenseStatus.Expired);
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        await _handler.Handle(new RenewLicenseCommand("LIC_1", DateTime.UtcNow.AddYears(1)), CancellationToken.None);

        license.Status.Should().Be(LicenseStatus.Assigned);
        license.AssignedUserId.Should().Be("USER_5");
    }

    [Fact]
    public async Task Handle_RenewLicense_ShouldKeepAssignedUserIntact()
    {
        var license = CreateLicense(assignedUser: "USER_5");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        await _handler.Handle(new RenewLicenseCommand("LIC_1", DateTime.UtcNow.AddYears(3)), CancellationToken.None);

        license.Status.Should().Be(LicenseStatus.Assigned);
        license.AssignedUserId.Should().Be("USER_5");
        license.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddYears(3), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_CreateSubscription_ShouldReturnMappedDtoWithMonthlyRenewal()
    {
        var result = await _handler.Handle(new CreateSubscriptionCommand("ORG_1", "PLAN_BUSINESS"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription created.");
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.PlanId.Should().Be("PLAN_BUSINESS");
        result.Data!.Status.Should().Be(SubscriptionStatus.Active.ToString());
        result.Data!.BillingCycle.Should().Be("Monthly");
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeGreaterThan(TimeSpan.FromDays(27));
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeLessThan(TimeSpan.FromDays(32));
    }

    [Fact]
    public async Task Handle_CreateSubscription_WithYearlyCycle_ShouldRenewInOneYear()
    {
        var result = await _handler.Handle(new CreateSubscriptionCommand("ORG_1", "PLAN_ENTERPRISE", "Yearly"), CancellationToken.None);

        result.Data!.BillingCycle.Should().Be("Yearly");
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeGreaterThan(TimeSpan.FromDays(360));
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeLessThan(TimeSpan.FromDays(370));
    }

    [Fact]
    public async Task Handle_CreateSubscription_ShouldPropagateTenantIdFromContext()
    {
        Subscription? created = null;
        _mockSubscriptionRepo.Setup(x => x.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => created = s)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateSubscriptionCommand("ORG_1", "PLAN_BUSINESS"), CancellationToken.None);

        created!.TenantId.Should().Be("TENANT_TEST");
        created!.PaymentHistoryLog.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreateSubscription_WithNonYearlyCycle_ShouldDefaultToMonthlyRenewal()
    {
        var result = await _handler.Handle(new CreateSubscriptionCommand("ORG_1", "PLAN_BUSINESS", "Quarterly"), CancellationToken.None);

        result.Data!.BillingCycle.Should().Be("Quarterly");
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeGreaterThan(TimeSpan.FromDays(27));
        (result.Data!.RenewalDate - DateTime.UtcNow).Should().BeLessThan(TimeSpan.FromDays(32));
    }

    [Fact]
    public async Task Handle_CreateSubscription_ShouldAddOnceAndPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new CreateSubscriptionCommand("ORG_1", "PLAN_BUSINESS"), cts.Token);

        _mockSubscriptionRepo.Verify(x => x.AddAsync(It.IsAny<Subscription>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_RenewSubscription_ShouldReturnTrueAndAdvanceRenewalDate()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        var original = sub.RenewalDate;
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription renewed.");
        result.Data.Should().BeTrue();
        sub.RenewalDate.Should().Be(original.AddMonths(1));
        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.PaymentHistoryLog.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_RenewSubscription_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var act = async () => await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Subscription*SUB_1*");
    }

    [Fact]
    public async Task Handle_RenewSubscription_OnYearlySubscription_ShouldAdvanceOneYear()
    {
        var sub = new Subscription("ORG_1", "PLAN_ENTERPRISE", "Yearly", "TENANT_TEST");
        var original = sub.RenewalDate;
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);

        sub.RenewalDate.Should().Be(original.AddYears(1));
    }

    [Fact]
    public async Task Handle_RenewSubscription_Twice_ShouldAppendTwoLogEntries()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);
        await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);

        sub.PaymentHistoryLog.Should().HaveCount(2);
        _mockSubscriptionRepo.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_RenewSubscription_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST"));

        await _handler.Handle(new RenewSubscriptionCommand("SUB_1"), CancellationToken.None);

        _mockSubscriptionRepo.Verify(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockSubscriptionRepo.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChangeSubscriptionPlan_ShouldReturnTrueAndUpdatePlan()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new ChangeSubscriptionPlanCommand("SUB_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription plan changed.");
        result.Data.Should().BeTrue();
        sub.PlanId.Should().Be("PLAN_ENTERPRISE");
        sub.PaymentHistoryLog.Should().HaveCount(1);
        sub.PaymentHistoryLog[0].Should().Contain("PLAN_ENTERPRISE");
    }

    [Fact]
    public async Task Handle_ChangeSubscriptionPlan_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var act = async () => await _handler.Handle(new ChangeSubscriptionPlanCommand("SUB_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Subscription*SUB_1*");
    }

    [Fact]
    public async Task Handle_ChangeSubscriptionPlan_ToSamePlan_ShouldStillSucceed()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new ChangeSubscriptionPlanCommand("SUB_1", "PLAN_BUSINESS"), CancellationToken.None);

        result.Data.Should().BeTrue();
        sub.PlanId.Should().Be("PLAN_BUSINESS");
        sub.PaymentHistoryLog.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ChangeSubscriptionPlan_ShouldPersistModifiedEntity()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        await _handler.Handle(new ChangeSubscriptionPlanCommand("SUB_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        _mockSubscriptionRepo.Verify(x => x.UpdateAsync(sub, It.IsAny<CancellationToken>()), Times.Once);
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_ChangeSubscriptionPlan_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST"));

        await _handler.Handle(new ChangeSubscriptionPlanCommand("SUB_1", "PLAN_ENTERPRISE"), CancellationToken.None);

        _mockSubscriptionRepo.Verify(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockSubscriptionRepo.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateInvitation_ShouldReturnMappedDtoAndSuccessMessage()
    {
        var result = await _handler.Handle(new CreateInvitationCommand("user@lifebalance.app"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Invitation generated and notification dispatched.");
        result.Data!.TargetEmail.Should().Be("user@lifebalance.app");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data!.Status.Should().Be(InvitationStatus.Pending.ToString());
        result.Data!.Role.Should().Be(MemberRole.Member.ToString());
        (result.Data!.ExpiresAt - DateTime.UtcNow).Should().BeGreaterThan(TimeSpan.FromDays(6));
        (result.Data!.ExpiresAt - DateTime.UtcNow).Should().BeLessThan(TimeSpan.FromDays(8));
    }

    [Fact]
    public async Task Handle_CreateInvitation_ShouldDispatchNotificationWithTokenLink()
    {
        string? sentEmail = null;
        string? sentLink = null;
        string? sentTenant = null;
        _mockNotificationClient.Setup(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((e, l, t, _) => { sentEmail = e; sentLink = l; sentTenant = t; })
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CreateInvitationCommand("user@lifebalance.app"), CancellationToken.None);

        _mockNotificationClient.Verify(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        sentEmail.Should().Be("user@lifebalance.app");
        sentTenant.Should().Be("TENANT_TEST");
        sentLink.Should().Be($"https://lifebalance.app/invite/{result.Data!.Token}");
    }

    [Fact]
    public async Task Handle_CreateInvitation_ShouldPropagateOrganizationAndFamilyIds()
    {
        var result = await _handler.Handle(new CreateInvitationCommand("user@lifebalance.app", "ORG_1", "FAM_1"), CancellationToken.None);

        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.FamilyId.Should().Be("FAM_1");
    }

    [Fact]
    public async Task Handle_CreateInvitation_ShouldPersistBeforeDispatchingNotification()
    {
        var sequence = new MockSequence();
        _mockInvitationRepo.InSequence(sequence).Setup(x => x.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockNotificationClient.InSequence(sequence).Setup(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _handler.Handle(new CreateInvitationCommand("user@lifebalance.app"), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockNotificationClient.Verify(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateInvitation_WhenNotificationFails_ShouldNotPersistInvitation()
    {
        _mockNotificationClient.Setup(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("smtp down"));

        var act = async () => await _handler.Handle(new CreateInvitationCommand("user@lifebalance.app"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        _mockInvitationRepo.Verify(x => x.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AcceptInvitation_ShouldReturnTrueAndSetAcceptedStatus()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        var result = await _handler.Handle(new AcceptInvitationCommand(inv.Token), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Invitation accepted.");
        result.Data.Should().BeTrue();
        inv.Status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public async Task Handle_AcceptInvitation_WhenTokenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Invitation>());

        var act = async () => await _handler.Handle(new AcceptInvitationCommand("TOKEN_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Invitation*TOKEN_UNKNOWN*");
    }

    [Fact]
    public async Task Handle_AcceptInvitation_WhenAlreadyAccepted_ShouldThrowInvalidOperationException()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        inv.Accept();
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        var act = async () => await _handler.Handle(new AcceptInvitationCommand(inv.Token), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only pending invitations can be accepted.");
    }

    [Fact]
    public async Task Handle_AcceptInvitation_WhenExpired_ShouldThrowInvalidOperationException()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        typeof(Invitation).GetProperty(nameof(Invitation.ExpiresAt))!.SetValue(inv, DateTime.UtcNow.AddDays(-1));
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        var act = async () => await _handler.Handle(new AcceptInvitationCommand(inv.Token), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invitation has expired.");
        inv.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public async Task Handle_AcceptInvitation_ShouldFindByTokenAndUpdateOnce()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        Expression<Func<Invitation, bool>>? capturedPredicate = null;
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Invitation, bool>>, CancellationToken>((p, _) => capturedPredicate = p)
            .ReturnsAsync(new[] { inv });

        await _handler.Handle(new AcceptInvitationCommand(inv.Token), CancellationToken.None);

        var other = new Invitation("other@x.com", "TENANT_TEST", "ORG_1");
        capturedPredicate!.Compile()(inv).Should().BeTrue();
        capturedPredicate!.Compile()(other).Should().BeFalse();
        _mockInvitationRepo.Verify(x => x.UpdateAsync(inv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectInvitation_ShouldReturnTrueAndSetRejectedStatus()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        var result = await _handler.Handle(new RejectInvitationCommand(inv.Token), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Invitation rejected.");
        result.Data.Should().BeTrue();
        inv.Status.Should().Be(InvitationStatus.Rejected);
    }

    [Fact]
    public async Task Handle_RejectInvitation_WhenTokenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Invitation>());

        var act = async () => await _handler.Handle(new RejectInvitationCommand("TOKEN_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Invitation*TOKEN_UNKNOWN*");
    }

    [Fact]
    public async Task Handle_RejectInvitation_WhenAlreadyRejected_ShouldThrowInvalidOperationException()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        inv.Reject();
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        var act = async () => await _handler.Handle(new RejectInvitationCommand(inv.Token), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only pending invitations can be rejected.");
    }

    [Fact]
    public async Task Handle_RejectInvitation_ShouldFindByTokenAndUpdateOnce()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        await _handler.Handle(new RejectInvitationCommand(inv.Token), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvitationRepo.Verify(x => x.UpdateAsync(inv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectInvitation_ShouldNotDispatchNotification()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Invitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { inv });

        await _handler.Handle(new RejectInvitationCommand(inv.Token), CancellationToken.None);

        _mockNotificationClient.Verify(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancelInvitation_ShouldReturnTrueAndSetCanceledStatus()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var result = await _handler.Handle(new CancelInvitationCommand("INV_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Invitation canceled.");
        result.Data.Should().BeTrue();
        inv.Status.Should().Be(InvitationStatus.Canceled);
    }

    [Fact]
    public async Task Handle_CancelInvitation_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync((Invitation?)null);

        var act = async () => await _handler.Handle(new CancelInvitationCommand("INV_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Invitation*INV_1*");
    }

    [Fact]
    public async Task Handle_CancelInvitation_OnCanceledInvitation_ShouldBeIdempotent()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        inv.Cancel();
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var result = await _handler.Handle(new CancelInvitationCommand("INV_1"), CancellationToken.None);

        result.Data.Should().BeTrue();
        inv.Status.Should().Be(InvitationStatus.Canceled);
    }

    [Fact]
    public async Task Handle_CancelInvitation_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1"));

        await _handler.Handle(new CancelInvitationCommand("INV_1"), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockInvitationRepo.Verify(x => x.UpdateAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CancelInvitation_ShouldPersistModifiedEntity()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        await _handler.Handle(new CancelInvitationCommand("INV_1"), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.UpdateAsync(inv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResendInvitation_ShouldReturnTrueAndRegenerateToken()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        var originalToken = inv.Token;
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var result = await _handler.Handle(new ResendInvitationCommand("INV_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Invitation resent.");
        result.Data.Should().BeTrue();
        inv.Token.Should().NotBe(originalToken);
        inv.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_ResendInvitation_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync((Invitation?)null);

        var act = async () => await _handler.Handle(new ResendInvitationCommand("INV_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Invitation*INV_1*");
    }

    [Fact]
    public async Task Handle_ResendInvitation_ShouldDispatchNotificationWithNewToken()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);
        string? sentLink = null;
        _mockNotificationClient.Setup(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, l, _, _) => sentLink = l)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new ResendInvitationCommand("INV_1"), CancellationToken.None);

        sentLink.Should().Be($"https://lifebalance.app/invite/{inv.Token}");
    }

    [Fact]
    public async Task Handle_ResendInvitation_ShouldUpdateBeforeDispatchingNotification()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        var sequence = new MockSequence();
        _mockInvitationRepo.InSequence(sequence).Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);
        _mockInvitationRepo.InSequence(sequence).Setup(x => x.UpdateAsync(inv, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockNotificationClient.InSequence(sequence).Setup(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _handler.Handle(new ResendInvitationCommand("INV_1"), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.UpdateAsync(inv, It.IsAny<CancellationToken>()), Times.Once);
        _mockNotificationClient.Verify(x => x.SendInvitationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResendInvitation_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1"));

        await _handler.Handle(new ResendInvitationCommand("INV_1"), CancellationToken.None);

        _mockInvitationRepo.Verify(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockInvitationRepo.Verify(x => x.UpdateAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
