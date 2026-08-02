using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.DepartmentsAndTeams;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class DepartmentAndTeamQueryHandlerTests
{
    private readonly Mock<IRepository<Department>> _mockDeptRepo;
    private readonly Mock<IRepository<Team>> _mockTeamRepo;
    private readonly DepartmentAndTeamQueryHandler _handler;

    public DepartmentAndTeamQueryHandlerTests()
    {
        _mockDeptRepo = new Mock<IRepository<Department>>();
        _mockTeamRepo = new Mock<IRepository<Team>>();

        _handler = new DepartmentAndTeamQueryHandler(_mockDeptRepo.Object, _mockTeamRepo.Object);
    }

    private static Department CreateDept(string id = "DEPT_1", string name = "Engineering")
    {
        var dept = new Department("ORG_1", name, "Builds stuff", "TENANT_TEST", "USER_MGR", "DEPT_0");
        dept.AddMember("USER_2");
        dept.AddMember("USER_3");
        return dept;
    }

    private static Team CreateTeam(string id = "TEAM_1", string name = "Core")
    {
        var team = new Team("ORG_1", name, "TENANT_TEST", "DEPT_1", "USER_LEADER");
        team.AddMember("USER_2");
        return team;
    }

    [Fact]
    public async Task Handle_GetDepartmentById_ShouldReturnMappedDto()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new GetDepartmentByIdQuery("DEPT_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(dept.Id);
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.Name.Should().Be("Engineering");
        result.Data!.Description.Should().Be("Builds stuff");
        result.Data!.ManagerUserId.Should().Be("USER_MGR");
        result.Data!.ParentDepartmentId.Should().Be("DEPT_0");
        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_2", "USER_3" });
        result.Data!.CreatedAt.Should().Be(dept.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetDepartmentById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var act = async () => await _handler.Handle(new GetDepartmentByIdQuery("DEPT_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Department*DEPT_1*");
    }

    [Fact]
    public async Task Handle_GetDepartmentById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", cts.Token)).ReturnsAsync(CreateDept());

        await _handler.Handle(new GetDepartmentByIdQuery("DEPT_1"), cts.Token);

        _mockDeptRepo.Verify(x => x.GetByIdAsync("DEPT_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetDepartmentById_WithNoMembers_ShouldReturnEmptyMemberList()
    {
        var dept = new Department("ORG_1", "Empty", "desc", "TENANT_TEST");
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new GetDepartmentByIdQuery("DEPT_1"), CancellationToken.None);

        result.Data!.MemberUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetDepartmentById_WithNullManager_ShouldReturnNullManager()
    {
        var dept = new Department("ORG_1", "Eng", "desc", "TENANT_TEST");
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new GetDepartmentByIdQuery("DEPT_1"), CancellationToken.None);

        result.Data!.ManagerUserId.Should().BeNull();
        result.Data!.ParentDepartmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GetDepartmentsPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Department> { CreateDept("DEPT_1", "Engineering"), CreateDept("DEPT_2", "Sales") };
        _mockDeptRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Department, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Department>, long))(items, 11L));

        var result = await _handler.Handle(new GetDepartmentsPagedQuery("ORG_1", 1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(11);
        result.Data!.PageIndex.Should().Be(1);
        result.Data!.PageSize.Should().Be(10);
        result.Data!.Items.Last().Name.Should().Be("Sales");
    }

    [Fact]
    public async Task Handle_GetDepartmentsPaged_ShouldFilterByOrganizationId()
    {
        Expression<Func<Department, bool>>? capturedPredicate = null;
        _mockDeptRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Department, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Department, bool>>, int, int, Expression<Func<Department, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Department>, long))(new List<Department>(), 0L));

        await _handler.Handle(new GetDepartmentsPagedQuery("ORG_1"), CancellationToken.None);

        var compiled = capturedPredicate!.Compile();
        compiled(new Department("ORG_1", "Eng", "d", "T")).Should().BeTrue();
        compiled(new Department("ORG_2", "Eng", "d", "T")).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetDepartmentsPaged_ShouldComputePagingProperties()
    {
        _mockDeptRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Department, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Department>, long))(new List<Department>(), 25L));

        var result = await _handler.Handle(new GetDepartmentsPagedQuery("ORG_1", 3, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeTrue();
        result.Data!.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetDepartmentsPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockDeptRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Department, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Department>, long))(new List<Department>(), 0L));

        var result = await _handler.Handle(new GetDepartmentsPagedQuery("ORG_1"), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetDepartmentsPaged_ShouldPropagatePagingArguments()
    {
        int capturedPageIndex = 0;
        int capturedPageSize = 0;
        _mockDeptRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Department, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Department, bool>>, int, int, Expression<Func<Department, object>>, bool, CancellationToken>((_, pi, ps, _, _, _) => { capturedPageIndex = pi; capturedPageSize = ps; })
            .ReturnsAsync(((IEnumerable<Department>, long))(new List<Department>(), 0L));

        await _handler.Handle(new GetDepartmentsPagedQuery("ORG_1", 2, 25), CancellationToken.None);

        capturedPageIndex.Should().Be(2);
        capturedPageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_GetTeamById_ShouldReturnMappedDto()
    {
        var team = CreateTeam();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _handler.Handle(new GetTeamByIdQuery("TEAM_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(team.Id);
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.DepartmentId.Should().Be("DEPT_1");
        result.Data!.Name.Should().Be("Core");
        result.Data!.LeaderUserId.Should().Be("USER_LEADER");
        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_2" });
        result.Data!.CreatedAt.Should().Be(team.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetTeamById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Team?)null);

        var act = async () => await _handler.Handle(new GetTeamByIdQuery("TEAM_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Team*TEAM_1*");
    }

    [Fact]
    public async Task Handle_GetTeamById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", cts.Token)).ReturnsAsync(CreateTeam());

        await _handler.Handle(new GetTeamByIdQuery("TEAM_1"), cts.Token);

        _mockTeamRepo.Verify(x => x.GetByIdAsync("TEAM_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetTeamById_WithNullDepartmentAndLeader_ShouldReturnNulls()
    {
        var team = new Team("ORG_1", "Solo", "TENANT_TEST");
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _handler.Handle(new GetTeamByIdQuery("TEAM_1"), CancellationToken.None);

        result.Data!.DepartmentId.Should().BeNull();
        result.Data!.LeaderUserId.Should().BeNull();
        result.Data!.MemberUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetTeamById_ShouldPropagateMemberList()
    {
        var team = new Team("ORG_1", "Core", "TENANT_TEST", "DEPT_1");
        team.AddMember("USER_1");
        team.AddMember("USER_2");
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _handler.Handle(new GetTeamByIdQuery("TEAM_1"), CancellationToken.None);

        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_1", "USER_2" });
    }

    [Fact]
    public async Task Handle_GetTeamsPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Team> { CreateTeam("TEAM_1", "Core"), CreateTeam("TEAM_2", "QA") };
        _mockTeamRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Team>, long))(items, 7L));

        var result = await _handler.Handle(new GetTeamsPagedQuery("ORG_1", 1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(7);
        result.Data!.PageIndex.Should().Be(1);
        result.Data!.PageSize.Should().Be(10);
        result.Data!.Items.Last().Name.Should().Be("QA");
    }

    [Fact]
    public async Task Handle_GetTeamsPaged_ShouldFilterByOrganizationId()
    {
        Expression<Func<Team, bool>>? capturedPredicate = null;
        _mockTeamRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Team, bool>>, int, int, Expression<Func<Team, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Team>, long))(new List<Team>(), 0L));

        await _handler.Handle(new GetTeamsPagedQuery("ORG_1"), CancellationToken.None);

        var compiled = capturedPredicate!.Compile();
        compiled(new Team("ORG_1", "Core", "T")).Should().BeTrue();
        compiled(new Team("ORG_2", "Core", "T")).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetTeamsPaged_ShouldComputePagingProperties()
    {
        _mockTeamRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Team>, long))(new List<Team>(), 25L));

        var result = await _handler.Handle(new GetTeamsPagedQuery("ORG_1", 1, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeFalse();
        result.Data!.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetTeamsPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockTeamRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Team>, long))(new List<Team>(), 0L));

        var result = await _handler.Handle(new GetTeamsPagedQuery("ORG_1"), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetTeamsPaged_ShouldPropagatePagingArguments()
    {
        int capturedPageIndex = 0;
        int capturedPageSize = 0;
        _mockTeamRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Team, bool>>, int, int, Expression<Func<Team, object>>, bool, CancellationToken>((_, pi, ps, _, _, _) => { capturedPageIndex = pi; capturedPageSize = ps; })
            .ReturnsAsync(((IEnumerable<Team>, long))(new List<Team>(), 0L));

        await _handler.Handle(new GetTeamsPagedQuery("ORG_1", 2, 25), CancellationToken.None);

        capturedPageIndex.Should().Be(2);
        capturedPageSize.Should().Be(25);
    }
}
