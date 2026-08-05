using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.SaaSPlans;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class SaaSPlanCommandHandlerTests
{
    private readonly Mock<IRepository<SaaSPlan>> _plans = new();
    private readonly Mock<IRepository<AuditLog>> _audits = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly SaaSPlanCommandHandler _handler;

    public SaaSPlanCommandHandlerTests()
    {
        _tenantContext.SetupGet(context => context.UserId).Returns("USER_1");
        _tenantContext.SetupGet(context => context.CorrelationId).Returns("CORRELATION_1");
        _handler = new SaaSPlanCommandHandler(_plans.Object, _audits.Object, _tenantContext.Object);
    }

    [Fact]
    public async Task Create_ShouldPersistFrontendContractAndAudit()
    {
        SaaSPlan? persisted = null;
        AuditLog? audit = null;
        _plans.Setup(repo => repo.AddAsync(It.IsAny<SaaSPlan>(), It.IsAny<CancellationToken>()))
            .Callback<SaaSPlan, CancellationToken>((plan, _) => persisted = plan);
        _audits.Setup(repo => repo.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, _) => audit = log);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.Data!.Tier.Should().Be("Corporativo");
        result.Data.Currency.Should().Be("MXN");
        result.Data.IsHighlighted.Should().BeTrue();
        result.Data.Features.Should().ContainSingle().Which.Should().Be("25 licencias");
        persisted.Should().NotBeNull();
        audit.Should().Match<AuditLog>(log => log.UserId == "USER_1" && log.Action == "Create"
            && log.EntityId == persisted!.Id && log.CorrelationId == "CORRELATION_1");
    }

    [Fact]
    public async Task Create_ShouldMapIndividualTierToPersonalDomainTier()
    {
        var command = CreateCommand() with { Tier = "Individual" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.Tier.Should().Be("Individual");
    }

    [Fact]
    public async Task Create_WithInvalidTier_ShouldThrowDomainException()
    {
        var act = () => _handler.Handle(CreateCommand() with { Tier = "Invalid" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        _plans.Verify(repo => repo.AddAsync(It.IsAny<SaaSPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithoutNameIdentifier_ShouldNotMutateData()
    {
        _tenantContext.SetupGet(context => context.UserId).Returns((string?)null);

        var act = () => _handler.Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _plans.Verify(repo => repo.AddAsync(It.IsAny<SaaSPlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldModifyAndAuditPlan()
    {
        var plan = CreatePlan();
        _plans.Setup(repo => repo.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        var command = new UpdateSaaSPlanCommand(plan.Id, "Enterprise", "Enterprise", 0, 0, "usd",
            true, false, ["Soporte dedicado"], CreateLimits());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.Name.Should().Be("Enterprise");
        result.Data.Currency.Should().Be("USD");
        result.Data.IsCustomPricing.Should().BeTrue();
        _plans.Verify(repo => repo.UpdateAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
        _audits.Verify(repo => repo.AddAsync(It.Is<AuditLog>(log => log.Action == "Update"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true, "Activate")]
    [InlineData(false, "Deactivate")]
    public async Task SetActive_ShouldToggleAndAudit(bool active, string action)
    {
        var plan = CreatePlan();
        if (active) plan.Deactivate();
        _plans.Setup(repo => repo.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        await _handler.Handle(new SetSaaSPlanActiveCommand(plan.Id, active), CancellationToken.None);

        plan.IsActive.Should().Be(active);
        _audits.Verify(repo => repo.AddAsync(It.Is<AuditLog>(log => log.Action == action), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenMissing_ShouldThrowNotFound()
    {
        _plans.Setup(repo => repo.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((SaaSPlan?)null);
        var command = new UpdateSaaSPlanCommand("missing", "Plan", "Enterprise", 0, 0, "MXN",
            true, false, [], CreateLimits());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    private static CreateSaaSPlanCommand CreateCommand() => new(
        "Corporativo", "Corporativo", 29m, 290m, "mxn", false, true, [" 25 licencias "], CreateLimits());

    private static PlanLimitsDto CreateLimits() => new() { MaxLicenses = 25, MaxUsers = 25 };

    private static SaaSPlan CreatePlan() => new(
        "Corporativo", PlanTier.Business, 29m, 290m, PlanLimits.DefaultFree(), "MXN", false, true, ["25 licencias"]);
}

public class SaaSPlanQueryHandlerTests
{
    private readonly Mock<IRepository<SaaSPlan>> _plans = new();
    private readonly SaaSPlanQueryHandler _handler;

    public SaaSPlanQueryHandlerTests() => _handler = new SaaSPlanQueryHandler(_plans.Object);

    [Fact]
    public async Task GetActive_ShouldFilterActivePlansAndClampLimit()
    {
        Expression<Func<SaaSPlan, bool>>? filter = null;
        var plan = new SaaSPlan("Individual", PlanTier.Personal, 29m, 290m, PlanLimits.DefaultFree());
        _plans.Setup(repo => repo.GetPagedAsync(
                It.IsAny<Expression<Func<SaaSPlan, bool>>>(), 1, 100,
                It.IsAny<Expression<Func<SaaSPlan, object>>>(), false, It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<SaaSPlan, bool>>, int, int, Expression<Func<SaaSPlan, object>>?, bool, CancellationToken>(
                (predicate, _, _, _, _, _) => filter = predicate)
            .ReturnsAsync((new[] { plan }.AsEnumerable(), 1L));

        var result = await _handler.Handle(new GetActiveSaaSPlansQuery(500), CancellationToken.None);

        result.Data.Should().ContainSingle();
        filter.Should().NotBeNull();
        filter!.Compile()(plan).Should().BeTrue();
        plan.Deactivate();
        filter.Compile()(plan).Should().BeFalse();
    }

    [Fact]
    public async Task GetActive_WhenCatalogEmpty_ShouldReturnEmptyList()
    {
        _plans.Setup(repo => repo.GetPagedAsync(
                It.IsAny<Expression<Func<SaaSPlan, bool>>>(), 1, 100,
                It.IsAny<Expression<Func<SaaSPlan, object>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<SaaSPlan>(), 0L));

        var result = await _handler.Handle(new GetActiveSaaSPlansQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ShouldReturnFullContract()
    {
        var plan = new SaaSPlan("Enterprise", PlanTier.Enterprise, 0, 0, PlanLimits.DefaultEnterprise(),
            "MXN", true, false, ["Soporte dedicado"]);
        _plans.Setup(repo => repo.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var result = await _handler.Handle(new GetSaaSPlanByIdQuery(plan.Id), CancellationToken.None);

        result.Data.Should().Match<SaaSPlanDto>(dto => dto.IsCustomPricing && dto.Limits.MaxLicenses == 10000
            && dto.Features.Contains("Soporte dedicado"));
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldThrowNotFound()
    {
        _plans.Setup(repo => repo.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((SaaSPlan?)null);

        var act = () => _handler.Handle(new GetSaaSPlanByIdQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
