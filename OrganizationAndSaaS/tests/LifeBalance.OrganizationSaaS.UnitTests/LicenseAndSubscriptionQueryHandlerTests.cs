using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.LicensesAndSubscriptions;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using License = LifeBalance.OrganizationSaaS.Domain.Entities.License;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class LicenseAndSubscriptionQueryHandlerTests
{
    private readonly Mock<IRepository<License>> _mockLicenseRepo;
    private readonly Mock<IRepository<Subscription>> _mockSubscriptionRepo;
    private readonly Mock<IRepository<Invitation>> _mockInvitationRepo;
    private readonly LicenseAndSubscriptionQueryHandler _handler;

    public LicenseAndSubscriptionQueryHandlerTests()
    {
        _mockLicenseRepo = new Mock<IRepository<License>>();
        _mockSubscriptionRepo = new Mock<IRepository<Subscription>>();
        _mockInvitationRepo = new Mock<IRepository<Invitation>>();

        _handler = new LicenseAndSubscriptionQueryHandler(_mockLicenseRepo.Object, _mockSubscriptionRepo.Object, _mockInvitationRepo.Object);
    }

    [Fact]
    public async Task Handle_GetLicenseById_ShouldReturnMappedDto()
    {
        var license = new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var result = await _handler.Handle(new GetLicenseByIdQuery("LIC_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(license.Id);
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.LicenseKey.Should().Be(license.LicenseKey);
        result.Data!.Type.Should().Be("Standard");
        result.Data!.Status.Should().Be(LicenseStatus.Available.ToString());
        result.Data!.IssuedAt.Should().Be(license.IssuedAt);
        result.Data!.ExpiresAt.Should().Be(license.ExpiresAt);
    }

    [Fact]
    public async Task Handle_GetLicenseById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync((License?)null);

        var act = async () => await _handler.Handle(new GetLicenseByIdQuery("LIC_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*License*LIC_1*");
    }

    [Fact]
    public async Task Handle_GetLicenseById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", cts.Token)).ReturnsAsync(new License("ORG_1", "Pro", DateTime.UtcNow.AddYears(1), "T"));

        await _handler.Handle(new GetLicenseByIdQuery("LIC_1"), cts.Token);

        _mockLicenseRepo.Verify(x => x.GetByIdAsync("LIC_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetLicenseById_ShouldMapAssignedStatus()
    {
        var license = new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        license.AssignToUser("USER_5");
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(license);

        var result = await _handler.Handle(new GetLicenseByIdQuery("LIC_1"), CancellationToken.None);

        result.Data!.Status.Should().Be(LicenseStatus.Assigned.ToString());
        result.Data!.AssignedUserId.Should().Be("USER_5");
    }

    [Fact]
    public async Task Handle_GetLicenseById_WithUnassignedLicense_ShouldReturnNullAssignedUser()
    {
        _mockLicenseRepo.Setup(x => x.GetByIdAsync("LIC_1", It.IsAny<CancellationToken>())).ReturnsAsync(new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST"));

        var result = await _handler.Handle(new GetLicenseByIdQuery("LIC_1"), CancellationToken.None);

        result.Data!.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GetLicensesPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<License>
        {
            new("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST"),
            new("ORG_1", "Pro", DateTime.UtcNow.AddYears(1), "TENANT_TEST")
        };
        _mockLicenseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<License, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<License, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<License>, long))(items, 12L));

        var result = await _handler.Handle(new GetLicensesPagedQuery("ORG_1", 2, 5), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(12);
        result.Data!.PageIndex.Should().Be(2);
        result.Data!.PageSize.Should().Be(5);
        result.Data!.Items.First().OrganizationId.Should().Be("ORG_1");
    }

    [Fact]
    public async Task Handle_GetLicensesPaged_ShouldFilterByOrganizationId()
    {
        Expression<Func<License, bool>>? capturedPredicate = null;
        _mockLicenseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<License, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<License, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<License, bool>>, int, int, Expression<Func<License, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<License>, long))(new List<License>(), 0L));

        await _handler.Handle(new GetLicensesPagedQuery("ORG_1"), CancellationToken.None);

        var compiled = capturedPredicate!.Compile();
        compiled(new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "T")).Should().BeTrue();
        compiled(new License("ORG_2", "Standard", DateTime.UtcNow.AddYears(1), "T")).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetLicensesPaged_ShouldComputePagingProperties()
    {
        _mockLicenseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<License, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<License, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<License>, long))(new List<License>(), 25L));

        var result = await _handler.Handle(new GetLicensesPagedQuery("ORG_1", 3, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeTrue();
        result.Data!.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetLicensesPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockLicenseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<License, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<License, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<License>, long))(new List<License>(), 0L));

        var result = await _handler.Handle(new GetLicensesPagedQuery("ORG_1"), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
        result.Data!.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetLicensesPaged_ShouldPropagatePagingArguments()
    {
        Expression<Func<License, bool>>? capturedPredicate = null;
        int capturedPageIndex = 0;
        int capturedPageSize = 0;
        _mockLicenseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<License, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<License, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<License, bool>>, int, int, Expression<Func<License, object>>, bool, CancellationToken>((p, pi, ps, _, _, _) => { capturedPredicate = p; capturedPageIndex = pi; capturedPageSize = ps; })
            .ReturnsAsync(((IEnumerable<License>, long))(new List<License>(), 0L));

        await _handler.Handle(new GetLicensesPagedQuery("ORG_1", 2, 5), CancellationToken.None);

        capturedPageIndex.Should().Be(2);
        capturedPageSize.Should().Be(5);
        capturedPredicate.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetSubscriptionById_ShouldReturnMappedDto()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new GetSubscriptionByIdQuery("SUB_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(sub.Id);
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.PlanId.Should().Be("PLAN_BUSINESS");
        result.Data!.Status.Should().Be(SubscriptionStatus.Active.ToString());
        result.Data!.BillingCycle.Should().Be("Monthly");
        result.Data!.RenewalDate.Should().Be(sub.RenewalDate);
    }

    [Fact]
    public async Task Handle_GetSubscriptionById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var act = async () => await _handler.Handle(new GetSubscriptionByIdQuery("SUB_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Subscription*SUB_1*");
    }

    [Fact]
    public async Task Handle_GetSubscriptionById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", cts.Token)).ReturnsAsync(new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "T"));

        await _handler.Handle(new GetSubscriptionByIdQuery("SUB_1"), cts.Token);

        _mockSubscriptionRepo.Verify(x => x.GetByIdAsync("SUB_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetSubscriptionById_ShouldPropagatePaymentHistory()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        sub.Renew();
        sub.ChangePlan("PLAN_PRO");
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new GetSubscriptionByIdQuery("SUB_1"), CancellationToken.None);

        result.Data!.PaymentHistoryLog.Should().HaveCount(2);
        result.Data!.PaymentHistoryLog[1].Should().Contain("PLAN_PRO");
    }

    [Fact]
    public async Task Handle_GetSubscriptionById_ShouldPropagateCanceledStatus()
    {
        var sub = new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST");
        sub.Cancel();
        _mockSubscriptionRepo.Setup(x => x.GetByIdAsync("SUB_1", It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var result = await _handler.Handle(new GetSubscriptionByIdQuery("SUB_1"), CancellationToken.None);

        result.Data!.Status.Should().Be(SubscriptionStatus.Canceled.ToString());
    }

    [Fact]
    public async Task Handle_GetSubscriptionsPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Subscription>
        {
            new("ORG_1", "PLAN_BUSINESS", "Monthly", "TENANT_TEST"),
            new("ORG_2", "PLAN_PRO", "Yearly", "TENANT_TEST")
        };
        _mockSubscriptionRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Subscription, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscription, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Subscription>, long))(items, 20L));

        var result = await _handler.Handle(new GetSubscriptionsPagedQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(20);
        result.Data!.PageIndex.Should().Be(1);
        result.Data!.PageSize.Should().Be(10);
        result.Data!.Items.Last().BillingCycle.Should().Be("Yearly");
    }

    [Fact]
    public async Task Handle_GetSubscriptionsPaged_ShouldMatchAllSubscriptions()
    {
        Expression<Func<Subscription, bool>>? capturedPredicate = null;
        _mockSubscriptionRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Subscription, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscription, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Subscription, bool>>, int, int, Expression<Func<Subscription, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Subscription>, long))(new List<Subscription>(), 0L));

        await _handler.Handle(new GetSubscriptionsPagedQuery(1, 10), CancellationToken.None);

        capturedPredicate!.Compile()(new Subscription("ORG_1", "PLAN_BUSINESS", "Monthly", "T")).Should().BeTrue();
        capturedPredicate!.Compile()(new Subscription("ORG_9", "PLAN_X", "Monthly", "T")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetSubscriptionsPaged_ShouldComputePagingProperties()
    {
        _mockSubscriptionRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Subscription, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscription, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Subscription>, long))(new List<Subscription>(), 25L));

        var result = await _handler.Handle(new GetSubscriptionsPagedQuery(1, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeFalse();
        result.Data!.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetSubscriptionsPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockSubscriptionRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Subscription, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscription, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Subscription>, long))(new List<Subscription>(), 0L));

        var result = await _handler.Handle(new GetSubscriptionsPagedQuery(1, 10), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetSubscriptionsPaged_ShouldPropagatePagingArguments()
    {
        int capturedPageIndex = 0;
        int capturedPageSize = 0;
        _mockSubscriptionRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Subscription, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Subscription, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Subscription, bool>>, int, int, Expression<Func<Subscription, object>>, bool, CancellationToken>((_, pi, ps, _, _, _) => { capturedPageIndex = pi; capturedPageSize = ps; })
            .ReturnsAsync(((IEnumerable<Subscription>, long))(new List<Subscription>(), 0L));

        await _handler.Handle(new GetSubscriptionsPagedQuery(3, 25), CancellationToken.None);

        capturedPageIndex.Should().Be(3);
        capturedPageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_GetInvitationById_ShouldReturnMappedDto()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var result = await _handler.Handle(new GetInvitationByIdQuery("INV_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(inv.Id);
        result.Data!.TargetEmail.Should().Be("user@lifebalance.app");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.Token.Should().Be(inv.Token);
        result.Data!.Status.Should().Be(InvitationStatus.Pending.ToString());
        result.Data!.Role.Should().Be(MemberRole.Member.ToString());
        result.Data!.SentAt.Should().Be(inv.SentAt);
        result.Data!.ExpiresAt.Should().Be(inv.ExpiresAt);
    }

    [Fact]
    public async Task Handle_GetInvitationById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync((Invitation?)null);

        var act = async () => await _handler.Handle(new GetInvitationByIdQuery("INV_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Invitation*INV_1*");
    }

    [Fact]
    public async Task Handle_GetInvitationById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", cts.Token)).ReturnsAsync(new Invitation("user@lifebalance.app", "T"));

        await _handler.Handle(new GetInvitationByIdQuery("INV_1"), cts.Token);

        _mockInvitationRepo.Verify(x => x.GetByIdAsync("INV_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetInvitationById_WithNullScopes_ShouldReturnNullOrganizationAndFamily()
    {
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Invitation("user@lifebalance.app", "TENANT_TEST"));

        var result = await _handler.Handle(new GetInvitationByIdQuery("INV_1"), CancellationToken.None);

        result.Data!.OrganizationId.Should().BeNull();
        result.Data!.FamilyId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GetInvitationById_ShouldMapAcceptedStatus()
    {
        var inv = new Invitation("user@lifebalance.app", "TENANT_TEST", "ORG_1");
        inv.Accept();
        _mockInvitationRepo.Setup(x => x.GetByIdAsync("INV_1", It.IsAny<CancellationToken>())).ReturnsAsync(inv);

        var result = await _handler.Handle(new GetInvitationByIdQuery("INV_1"), CancellationToken.None);

        result.Data!.Status.Should().Be(InvitationStatus.Accepted.ToString());
    }

    [Fact]
    public async Task Handle_GetInvitationsPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Invitation>
        {
            new("a@x.com", "TENANT_TEST", "ORG_1"),
            new("b@x.com", "TENANT_TEST", null, "FAM_1")
        };
        _mockInvitationRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Invitation, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Invitation, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Invitation>, long))(items, 9L));

        var result = await _handler.Handle(new GetInvitationsPagedQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(9);
        result.Data!.Items.Last().FamilyId.Should().Be("FAM_1");
    }

    [Fact]
    public async Task Handle_GetInvitationsPaged_ShouldMatchAllInvitations()
    {
        Expression<Func<Invitation, bool>>? capturedPredicate = null;
        _mockInvitationRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Invitation, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Invitation, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Invitation, bool>>, int, int, Expression<Func<Invitation, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Invitation>, long))(new List<Invitation>(), 0L));

        await _handler.Handle(new GetInvitationsPagedQuery(1, 10), CancellationToken.None);

        capturedPredicate!.Compile()(new Invitation("a@x.com", "T")).Should().BeTrue();
        capturedPredicate!.Compile()(new Invitation("b@x.com", "T")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetInvitationsPaged_ShouldComputePagingProperties()
    {
        _mockInvitationRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Invitation, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Invitation, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Invitation>, long))(new List<Invitation>(), 25L));

        var result = await _handler.Handle(new GetInvitationsPagedQuery(2, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeTrue();
        result.Data!.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetInvitationsPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockInvitationRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Invitation, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Invitation, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Invitation>, long))(new List<Invitation>(), 0L));

        var result = await _handler.Handle(new GetInvitationsPagedQuery(1, 10), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetInvitationsPaged_ShouldPropagatePagingArguments()
    {
        int capturedPageIndex = 0;
        int capturedPageSize = 0;
        _mockInvitationRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Invitation, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Invitation, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Invitation, bool>>, int, int, Expression<Func<Invitation, object>>, bool, CancellationToken>((_, pi, ps, _, _, _) => { capturedPageIndex = pi; capturedPageSize = ps; })
            .ReturnsAsync(((IEnumerable<Invitation>, long))(new List<Invitation>(), 0L));

        await _handler.Handle(new GetInvitationsPagedQuery(4, 50), CancellationToken.None);

        capturedPageIndex.Should().Be(4);
        capturedPageSize.Should().Be(50);
    }
}
