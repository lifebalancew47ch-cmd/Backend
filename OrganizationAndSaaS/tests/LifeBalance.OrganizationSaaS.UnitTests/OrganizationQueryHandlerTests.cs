using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Features.Organizations;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;
using License = LifeBalance.OrganizationSaaS.Domain.Entities.License;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class OrganizationQueryHandlerTests
{
    private readonly Mock<IRepository<Organization>> _mockOrgRepo;
    private readonly Mock<IRepository<Department>> _mockDeptRepo;
    private readonly Mock<IRepository<Team>> _mockTeamRepo;
    private readonly Mock<IRepository<License>> _mockLicenseRepo;
    private readonly OrganizationQueryHandler _handler;

    public OrganizationQueryHandlerTests()
    {
        _mockOrgRepo = new Mock<IRepository<Organization>>();
        _mockDeptRepo = new Mock<IRepository<Department>>();
        _mockTeamRepo = new Mock<IRepository<Team>>();
        _mockLicenseRepo = new Mock<IRepository<License>>();

        _handler = new OrganizationQueryHandler(_mockOrgRepo.Object, _mockDeptRepo.Object, _mockTeamRepo.Object, _mockLicenseRepo.Object);
    }

    private static Organization CreateOrg(string id = "ORG_1", string name = "Initech Corp", string tenant = "TENANT_TEST")
        => new(name, "TAX999", "PLAN_BUSINESS", tenant, new ContactInfo { Email = "a@b.com" }, new Address { City = "CDMX" });

    [Fact]
    public async Task Handle_GetOrganizationById_ShouldReturnMappedDto()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new GetOrganizationByIdQuery("ORG_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(org.Id);
        result.Data!.Name.Should().Be("Initech Corp");
        result.Data!.TaxId.Should().Be("TAX999");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.Status.Should().Be(OrganizationStatus.Active.ToString());
        result.Data!.PlanId.Should().Be("PLAN_BUSINESS");
        result.Data!.ContactInfo.Email.Should().Be("a@b.com");
        result.Data!.Address.City.Should().Be("CDMX");
        result.Data!.CreatedAt.Should().Be(org.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetOrganizationById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new GetOrganizationByIdQuery("ORG_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_GetOrganizationById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", cts.Token)).ReturnsAsync(CreateOrg());

        await _handler.Handle(new GetOrganizationByIdQuery("ORG_1"), cts.Token);

        _mockOrgRepo.Verify(x => x.GetByIdAsync("ORG_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetOrganizationById_ShouldPropagateUpdatedAt()
    {
        var org = CreateOrg();
        org.Touch();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new GetOrganizationByIdQuery("ORG_1"), CancellationToken.None);

        result.Data!.UpdatedAt.Should().Be(org.UpdatedAt);
        result.Data!.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetOrganizationById_ShouldPropagateTenantId()
    {
        var org = CreateOrg(id: "ORG_9", tenant: "TENANT_XYZ");
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_9", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var result = await _handler.Handle(new GetOrganizationByIdQuery("ORG_9"), CancellationToken.None);

        result.Data!.TenantId.Should().Be("TENANT_XYZ");
        result.Data!.Id.Should().Be(org.Id);
    }

    [Fact]
    public async Task Handle_GetOrganizationsPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Organization> { CreateOrg("ORG_1"), CreateOrg("ORG_2", "Acme") };
        _mockOrgRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Organization, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Organization>, long))(items, 25L));

        var result = await _handler.Handle(new GetOrganizationsPagedQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(25);
        result.Data!.PageIndex.Should().Be(1);
        result.Data!.PageSize.Should().Be(10);
        result.Data!.Items.First().Name.Should().Be("Initech Corp");
    }

    [Fact]
    public async Task Handle_GetOrganizationsPaged_ShouldComputePagingProperties()
    {
        _mockOrgRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Organization, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Organization>, long))(new List<Organization>(), 25L));

        var result = await _handler.Handle(new GetOrganizationsPagedQuery(3, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeTrue();
        result.Data!.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetOrganizationsPaged_WithSearch_ShouldFilterByName()
    {
        Expression<Func<Organization, bool>>? capturedPredicate = null;
        _mockOrgRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Organization, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Organization, bool>>, int, int, Expression<Func<Organization, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Organization>, long))(new List<Organization>(), 0L));

        await _handler.Handle(new GetOrganizationsPagedQuery(1, 10, "Initech"), CancellationToken.None);

        var compiled = capturedPredicate!.Compile();
        compiled(CreateOrg("ORG_1", "Initech Corp")).Should().BeTrue();
        compiled(CreateOrg("ORG_2", "Acme")).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetOrganizationsPaged_WithoutSearch_ShouldMatchAll()
    {
        Expression<Func<Organization, bool>>? capturedPredicate = null;
        _mockOrgRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Organization, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Organization, bool>>, int, int, Expression<Func<Organization, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Organization>, long))(new List<Organization>(), 0L));

        await _handler.Handle(new GetOrganizationsPagedQuery(1, 10, null), CancellationToken.None);

        var compiled = capturedPredicate!.Compile();
        compiled(CreateOrg("ORG_1", "Initech Corp")).Should().BeTrue();
        compiled(CreateOrg("ORG_2", "Acme")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetOrganizationsPaged_ShouldOrderByCreatedAtDescending()
    {
        Expression<Func<Organization, object>>? capturedOrderBy = null;
        bool capturedDescending = false;
        _mockOrgRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Organization, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Organization, bool>>, int, int, Expression<Func<Organization, object>>, bool, CancellationToken>((_, _, _, o, sd, _) => { capturedOrderBy = o; capturedDescending = sd; })
            .ReturnsAsync(((IEnumerable<Organization>, long))(new List<Organization>(), 0L));

        await _handler.Handle(new GetOrganizationsPagedQuery(1, 10), CancellationToken.None);

        capturedDescending.Should().BeTrue();
        capturedOrderBy.Should().NotBeNull();
        var org = CreateOrg();
        capturedOrderBy!.Compile()(org).Should().Be(org.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetOrganizationStats_ShouldComputeCountsFromRepositories()
    {
        var org = CreateOrg();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(org);

        var dept1 = new Department("ORG_1", "Eng", "desc", "TENANT_TEST");
        dept1.AddMember("U1");
        var dept2 = new Department("ORG_1", "Sales", "desc", "TENANT_TEST");
        dept2.AddMember("U2");
        dept2.AddMember("U3");
        _mockDeptRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { dept1, dept2 });

        _mockTeamRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { new("ORG_1", "Core", "TENANT_TEST"), new("ORG_1", "QA", "TENANT_TEST"), new("ORG_1", "Ops", "TENANT_TEST") });

        var lic1 = new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        lic1.AssignToUser("U1");
        var lic2 = new License("ORG_1", "Standard", DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        lic2.AssignToUser("U2");
        var lic3 = new License("ORG_1", "Pro", DateTime.UtcNow.AddYears(1), "TENANT_TEST");
        _mockLicenseRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<License, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<License> { lic1, lic2, lic3 });

        var result = await _handler.Handle(new GetOrganizationStatsQuery("ORG_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.OrganizationId.Should().Be(org.Id);
        result.Data!.Name.Should().Be("Initech Corp");
        result.Data!.TotalDepartments.Should().Be(2);
        result.Data!.TotalTeams.Should().Be(3);
        result.Data!.TotalLicenses.Should().Be(3);
        result.Data!.ActiveLicenses.Should().Be(2);
        result.Data!.TotalMembers.Should().Be(3);
    }

    [Fact]
    public async Task Handle_GetOrganizationStats_WithNoChildEntities_ShouldReturnZeroCounts()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateOrg());
        _mockDeptRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Department>());
        _mockTeamRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Team>());
        _mockLicenseRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<License, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<License>());

        var result = await _handler.Handle(new GetOrganizationStatsQuery("ORG_1"), CancellationToken.None);

        result.Data!.TotalDepartments.Should().Be(0);
        result.Data!.TotalTeams.Should().Be(0);
        result.Data!.TotalLicenses.Should().Be(0);
        result.Data!.ActiveLicenses.Should().Be(0);
        result.Data!.TotalMembers.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetOrganizationStats_WhenOrganizationNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(new GetOrganizationStatsQuery("ORG_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Organization*ORG_1*");
    }

    [Fact]
    public async Task Handle_GetOrganizationStats_ShouldFilterChildEntitiesByOrganizationId()
    {
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateOrg());
        Expression<Func<Department, bool>>? deptPredicate = null;
        Expression<Func<Team, bool>>? teamPredicate = null;
        Expression<Func<License, bool>>? licensePredicate = null;
        _mockDeptRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Department, bool>>, CancellationToken>((p, _) => deptPredicate = p)
            .ReturnsAsync(new List<Department>());
        _mockTeamRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Team, bool>>, CancellationToken>((p, _) => teamPredicate = p)
            .ReturnsAsync(new List<Team>());
        _mockLicenseRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<License, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<License, bool>>, CancellationToken>((p, _) => licensePredicate = p)
            .ReturnsAsync(new List<License>());

        await _handler.Handle(new GetOrganizationStatsQuery("ORG_1"), CancellationToken.None);

        deptPredicate!.Compile()(new Department("ORG_1", "Eng", "d", "T")).Should().BeTrue();
        deptPredicate!.Compile()(new Department("ORG_2", "Eng", "d", "T")).Should().BeFalse();
        teamPredicate!.Compile()(new Team("ORG_1", "Core", "T")).Should().BeTrue();
        teamPredicate!.Compile()(new Team("ORG_2", "Core", "T")).Should().BeFalse();
        licensePredicate!.Compile()(new License("ORG_1", "Pro", DateTime.UtcNow.AddDays(1), "T")).Should().BeTrue();
        licensePredicate!.Compile()(new License("ORG_2", "Pro", DateTime.UtcNow.AddDays(1), "T")).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetOrganizationStats_ShouldQueryAllThreeChildRepositories()
    {
        using var cts = new CancellationTokenSource();
        _mockOrgRepo.Setup(x => x.GetByIdAsync("ORG_1", cts.Token)).ReturnsAsync(CreateOrg());
        _mockDeptRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), cts.Token)).ReturnsAsync(new List<Department>());
        _mockTeamRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Team, bool>>>(), cts.Token)).ReturnsAsync(new List<Team>());
        _mockLicenseRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<License, bool>>>(), cts.Token)).ReturnsAsync(new List<License>());

        await _handler.Handle(new GetOrganizationStatsQuery("ORG_1"), cts.Token);

        _mockDeptRepo.Verify(x => x.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), cts.Token), Times.Once);
        _mockTeamRepo.Verify(x => x.FindAsync(It.IsAny<Expression<Func<Team, bool>>>(), cts.Token), Times.Once);
        _mockLicenseRepo.Verify(x => x.FindAsync(It.IsAny<Expression<Func<License, bool>>>(), cts.Token), Times.Once);
    }
}
