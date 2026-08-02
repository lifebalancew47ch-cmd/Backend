using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.DepartmentsAndTeams;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class DepartmentAndTeamCommandHandlerTests
{
    private readonly Mock<IRepository<Department>> _mockDeptRepo;
    private readonly Mock<IRepository<Team>> _mockTeamRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly DepartmentAndTeamCommandHandler _handler;

    public DepartmentAndTeamCommandHandlerTests()
    {
        _mockDeptRepo = new Mock<IRepository<Department>>();
        _mockTeamRepo = new Mock<IRepository<Team>>();
        _mockTenantContext = new Mock<ITenantContext>();

        _mockTenantContext.Setup(x => x.TenantId).Returns("TENANT_TEST");

        _handler = new DepartmentAndTeamCommandHandler(_mockDeptRepo.Object, _mockTeamRepo.Object, _mockTenantContext.Object);
    }

    private static Department CreateDept(string id = "DEPT_1")
    {
        var dept = new Department("ORG_1", "Engineering", "Builds stuff", "TENANT_TEST", "USER_MGR", "DEPT_0");
        dept.AddMember("USER_2");
        return dept;
    }

    private static Team CreateTeam(string id = "TEAM_1")
    {
        var team = new Team("ORG_1", "Core", "TENANT_TEST", "DEPT_1", "USER_LEADER");
        team.AddMember("USER_2");
        return team;
    }

    [Fact]
    public async Task Handle_CreateDepartment_ShouldReturnMappedDtoAndSuccessMessage()
    {
        var result = await _handler.Handle(new CreateDepartmentCommand("ORG_1", "Engineering", "Builds stuff"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Department created.");
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.Name.Should().Be("Engineering");
        result.Data!.Description.Should().Be("Builds stuff");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.ManagerUserId.Should().BeNull();
        result.Data!.ParentDepartmentId.Should().BeNull();
        result.Data!.MemberUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreateDepartment_ShouldPropagateTenantIdFromContext()
    {
        Department? created = null;
        _mockDeptRepo.Setup(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Callback<Department, CancellationToken>((d, _) => created = d)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateDepartmentCommand("ORG_1", "Engineering", "desc"), CancellationToken.None);

        created!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_CreateDepartment_WithNullManagerAndParent_ShouldPersistNulls()
    {
        Department? created = null;
        _mockDeptRepo.Setup(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Callback<Department, CancellationToken>((d, _) => created = d)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateDepartmentCommand("ORG_1", "Engineering", "desc", null, null), CancellationToken.None);

        created!.ManagerUserId.Should().BeNull();
        created!.ParentDepartmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CreateDepartment_WithManagerAndParent_ShouldPropagateBoth()
    {
        Department? created = null;
        _mockDeptRepo.Setup(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Callback<Department, CancellationToken>((d, _) => created = d)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateDepartmentCommand("ORG_1", "Engineering", "desc", "USER_MGR", "DEPT_0"), CancellationToken.None);

        created!.ManagerUserId.Should().Be("USER_MGR");
        created!.ParentDepartmentId.Should().Be("DEPT_0");
    }

    [Fact]
    public async Task Handle_CreateDepartment_ShouldAddOnceAndPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new CreateDepartmentCommand("ORG_1", "Engineering", "desc"), cts.Token);

        _mockDeptRepo.Verify(x => x.AddAsync(It.IsAny<Department>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateDepartment_ShouldReturnUpdatedDto()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new UpdateDepartmentCommand("DEPT_1", "R&D", "Research", "USER_NEW_MGR", "DEPT_9"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Department updated.");
        result.Data!.Name.Should().Be("R&D");
        result.Data!.Description.Should().Be("Research");
        result.Data!.ManagerUserId.Should().Be("USER_NEW_MGR");
        result.Data!.ParentDepartmentId.Should().Be("DEPT_9");
    }

    [Fact]
    public async Task Handle_UpdateDepartment_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var act = async () => await _handler.Handle(new UpdateDepartmentCommand("DEPT_1", "R&D", "desc", null), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Department*DEPT_1*");
    }

    [Fact]
    public async Task Handle_UpdateDepartment_WithNullManager_ShouldClearManager()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new UpdateDepartmentCommand("DEPT_1", "R&D", "desc", null, null), CancellationToken.None);

        result.Data!.ManagerUserId.Should().BeNull();
        result.Data!.ParentDepartmentId.Should().BeNull();
        dept.MemberUserIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UpdateDepartment_ShouldPersistModifiedEntity()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);
        Department? updated = null;
        _mockDeptRepo.Setup(x => x.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Callback<Department, CancellationToken>((d, _) => updated = d)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateDepartmentCommand("DEPT_1", "R&D", "desc", null, null), CancellationToken.None);

        updated.Should().BeSameAs(dept);
        updated!.Name.Should().Be("R&D");
    }

    [Fact]
    public async Task Handle_UpdateDepartment_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateDept());

        await _handler.Handle(new UpdateDepartmentCommand("DEPT_1", "R&D", "desc", null, null), CancellationToken.None);

        _mockDeptRepo.Verify(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockDeptRepo.Verify(x => x.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteDepartment_ShouldReturnTrueAndSoftDelete()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateDept());

        var result = await _handler.Handle(new DeleteDepartmentCommand("DEPT_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Department deleted.");
        result.Data.Should().BeTrue();
        _mockDeptRepo.Verify(x => x.SoftDeleteAsync("DEPT_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteDepartment_ShouldPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", cts.Token)).ReturnsAsync(CreateDept());

        await _handler.Handle(new DeleteDepartmentCommand("DEPT_1"), cts.Token);

        _mockDeptRepo.Verify(x => x.SoftDeleteAsync("DEPT_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteDepartment_Twice_ShouldBeIdempotent()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateDept());

        await _handler.Handle(new DeleteDepartmentCommand("DEPT_1"), CancellationToken.None);
        var second = await _handler.Handle(new DeleteDepartmentCommand("DEPT_1"), CancellationToken.None);

        second.Data.Should().BeTrue();
        _mockDeptRepo.Verify(x => x.SoftDeleteAsync("DEPT_1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DeleteDepartment_WithUnknownId_ShouldThrowResourceNotFoundException()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var act = async () => await _handler.Handle(new DeleteDepartmentCommand("DEPT_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Department*DEPT_UNKNOWN*");
        _mockDeptRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeleteDepartment_ShouldSoftDeleteOnlyRequestedDepartment()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_9", It.IsAny<CancellationToken>())).ReturnsAsync(CreateDept("DEPT_9"));

        await _handler.Handle(new DeleteDepartmentCommand("DEPT_9"), CancellationToken.None);

        _mockDeptRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTeamRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AssignDepartmentMember_ShouldReturnTrueAndAddMember()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_9"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("User assigned to department.");
        result.Data.Should().BeTrue();
        dept.MemberUserIds.Should().Contain("USER_9");
    }

    [Fact]
    public async Task Handle_AssignDepartmentMember_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var act = async () => await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_9"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Department*DEPT_1*");
    }

    [Fact]
    public async Task Handle_AssignDepartmentMember_WhenAlreadyMember_ShouldSucceedWithoutDuplicates()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_2"), CancellationToken.None);

        result.Data.Should().BeTrue();
        dept.MemberUserIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_AssignDepartmentMember_ToMultipleUsers_ShouldAddAll()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_9"), CancellationToken.None);
        await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_10"), CancellationToken.None);

        dept.MemberUserIds.Should().Contain(new[] { "USER_9", "USER_10" });
        dept.MemberUserIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_AssignDepartmentMember_ShouldPersistModifiedEntity()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        await _handler.Handle(new AssignDepartmentMemberCommand("DEPT_1", "USER_9"), CancellationToken.None);

        _mockDeptRepo.Verify(x => x.UpdateAsync(dept, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveDepartmentMember_ShouldReturnTrueAndRemoveMember()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new RemoveDepartmentMemberCommand("DEPT_1", "USER_2"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("User removed from department.");
        result.Data.Should().BeTrue();
        dept.MemberUserIds.Should().NotContain("USER_2");
    }

    [Fact]
    public async Task Handle_RemoveDepartmentMember_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync((Department?)null);

        var act = async () => await _handler.Handle(new RemoveDepartmentMemberCommand("DEPT_1", "USER_2"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Department*DEPT_1*");
    }

    [Fact]
    public async Task Handle_RemoveDepartmentMember_WhenNotMember_ShouldSucceedWithoutChanges()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        var result = await _handler.Handle(new RemoveDepartmentMemberCommand("DEPT_1", "USER_UNKNOWN"), CancellationToken.None);

        result.Data.Should().BeTrue();
        dept.MemberUserIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_RemoveDepartmentMember_ShouldPersistModifiedEntity()
    {
        var dept = CreateDept();
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        await _handler.Handle(new RemoveDepartmentMemberCommand("DEPT_1", "USER_2"), CancellationToken.None);

        _mockDeptRepo.Verify(x => x.UpdateAsync(dept, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveDepartmentMember_LastMember_ShouldLeaveEmptyList()
    {
        var dept = new Department("ORG_1", "Solo", "desc", "TENANT_TEST");
        dept.AddMember("USER_1");
        _mockDeptRepo.Setup(x => x.GetByIdAsync("DEPT_1", It.IsAny<CancellationToken>())).ReturnsAsync(dept);

        await _handler.Handle(new RemoveDepartmentMemberCommand("DEPT_1", "USER_1"), CancellationToken.None);

        dept.MemberUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreateTeam_ShouldReturnMappedDtoAndSuccessMessage()
    {
        var result = await _handler.Handle(new CreateTeamCommand("ORG_1", "Core"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Team created.");
        result.Data!.OrganizationId.Should().Be("ORG_1");
        result.Data!.Name.Should().Be("Core");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.DepartmentId.Should().BeNull();
        result.Data!.LeaderUserId.Should().BeNull();
        result.Data!.MemberUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreateTeam_ShouldPropagateTenantIdFromContext()
    {
        Team? created = null;
        _mockTeamRepo.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((t, _) => created = t)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateTeamCommand("ORG_1", "Core"), CancellationToken.None);

        created!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_CreateTeam_WithDepartmentAndLeader_ShouldPropagateBoth()
    {
        Team? created = null;
        _mockTeamRepo.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((t, _) => created = t)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateTeamCommand("ORG_1", "Core", "DEPT_1", "USER_LEADER"), CancellationToken.None);

        created!.DepartmentId.Should().Be("DEPT_1");
        created!.LeaderUserId.Should().Be("USER_LEADER");
    }

    [Fact]
    public async Task Handle_CreateTeam_WithNullDepartment_ShouldPersistNull()
    {
        Team? created = null;
        _mockTeamRepo.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((t, _) => created = t)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateTeamCommand("ORG_1", "Core", null, null), CancellationToken.None);

        created!.DepartmentId.Should().BeNull();
        created!.LeaderUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CreateTeam_ShouldAddOnceAndPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new CreateTeamCommand("ORG_1", "Core"), cts.Token);

        _mockTeamRepo.Verify(x => x.AddAsync(It.IsAny<Team>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateTeam_ShouldReturnUpdatedDto()
    {
        var team = CreateTeam();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _handler.Handle(new UpdateTeamCommand("TEAM_1", "Core Plus", "DEPT_2", "USER_NEW_LEADER"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Team updated.");
        result.Data!.Name.Should().Be("Core Plus");
        result.Data!.DepartmentId.Should().Be("DEPT_2");
        result.Data!.LeaderUserId.Should().Be("USER_NEW_LEADER");
    }

    [Fact]
    public async Task Handle_UpdateTeam_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Team?)null);

        var act = async () => await _handler.Handle(new UpdateTeamCommand("TEAM_1", "Core Plus", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Team*TEAM_1*");
    }

    [Fact]
    public async Task Handle_UpdateTeam_ShouldClearDepartmentAndLeaderWhenNull()
    {
        var team = CreateTeam();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _handler.Handle(new UpdateTeamCommand("TEAM_1", "Core Plus", null, null), CancellationToken.None);

        result.Data!.DepartmentId.Should().BeNull();
        result.Data!.LeaderUserId.Should().BeNull();
        team.MemberUserIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UpdateTeam_ShouldPersistModifiedEntity()
    {
        var team = CreateTeam();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(team);
        Team? updated = null;
        _mockTeamRepo.Setup(x => x.UpdateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((t, _) => updated = t)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateTeamCommand("TEAM_1", "Core Plus", "DEPT_2", null), CancellationToken.None);

        updated.Should().BeSameAs(team);
        updated!.DepartmentId.Should().Be("DEPT_2");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UpdateTeam_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateTeam());

        await _handler.Handle(new UpdateTeamCommand("TEAM_1", "Core Plus", null, null), CancellationToken.None);

        _mockTeamRepo.Verify(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockTeamRepo.Verify(x => x.UpdateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteTeam_ShouldReturnTrueAndSoftDelete()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateTeam());

        var result = await _handler.Handle(new DeleteTeamCommand("TEAM_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Team deleted.");
        result.Data.Should().BeTrue();
        _mockTeamRepo.Verify(x => x.SoftDeleteAsync("TEAM_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteTeam_ShouldPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", cts.Token)).ReturnsAsync(CreateTeam());

        await _handler.Handle(new DeleteTeamCommand("TEAM_1"), cts.Token);

        _mockTeamRepo.Verify(x => x.SoftDeleteAsync("TEAM_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteTeam_Twice_ShouldBeIdempotent()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateTeam());

        await _handler.Handle(new DeleteTeamCommand("TEAM_1"), CancellationToken.None);
        var second = await _handler.Handle(new DeleteTeamCommand("TEAM_1"), CancellationToken.None);

        second.Data.Should().BeTrue();
        _mockTeamRepo.Verify(x => x.SoftDeleteAsync("TEAM_1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DeleteTeam_WithUnknownId_ShouldThrowResourceNotFoundException()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((Team?)null);

        var act = async () => await _handler.Handle(new DeleteTeamCommand("TEAM_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Team*TEAM_UNKNOWN*");
        _mockTeamRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeleteTeam_ShouldSoftDeleteOnlyRequestedTeam()
    {
        _mockTeamRepo.Setup(x => x.GetByIdAsync("TEAM_9", It.IsAny<CancellationToken>())).ReturnsAsync(CreateTeam("TEAM_9"));

        await _handler.Handle(new DeleteTeamCommand("TEAM_9"), CancellationToken.None);

        _mockTeamRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDeptRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
