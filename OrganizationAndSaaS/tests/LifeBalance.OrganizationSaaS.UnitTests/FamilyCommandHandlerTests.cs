using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.Families;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class FamilyCommandHandlerTests
{
    private readonly Mock<IRepository<Family>> _mockFamilyRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly FamilyCommandHandler _handler;

    public FamilyCommandHandlerTests()
    {
        _mockFamilyRepo = new Mock<IRepository<Family>>();
        _mockTenantContext = new Mock<ITenantContext>();

        _mockTenantContext.Setup(x => x.TenantId).Returns("TENANT_TEST");

        _handler = new FamilyCommandHandler(_mockFamilyRepo.Object, _mockTenantContext.Object);
    }

    private static Family CreateFamily(string id = "FAM_1", int maxMembers = 6)
    {
        var family = new Family("Gomez Family", "USER_ADMIN", "TENANT_TEST", maxMembers);
        family.AddMember("USER_2");
        return family;
    }

    [Fact]
    public async Task Handle_CreateFamily_ShouldReturnMappedDtoAndSuccessMessage()
    {
        var result = await _handler.Handle(new CreateFamilyCommand("Gomez Family", "USER_ADMIN"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Family created successfully.");
        result.Data!.Name.Should().Be("Gomez Family");
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.AdministratorUserId.Should().Be("USER_ADMIN");
        result.Data!.MaxMembers.Should().Be(6);
        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_ADMIN" });
    }

    [Fact]
    public async Task Handle_CreateFamily_WithTenantFromContext_ShouldPersistTenant()
    {
        Family? created = null;
        _mockFamilyRepo.Setup(x => x.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
            .Callback<Family, CancellationToken>((f, _) => created = f)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateFamilyCommand("Gomez Family", "USER_ADMIN"), CancellationToken.None);

        created!.TenantId.Should().Be("TENANT_TEST");
    }

    [Fact]
    public async Task Handle_CreateFamily_WithMissingTenantId_ShouldGenerateTenantId()
    {
        _mockTenantContext.Setup(x => x.TenantId).Returns(" ");
        Family? created = null;
        _mockFamilyRepo.Setup(x => x.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
            .Callback<Family, CancellationToken>((f, _) => created = f)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateFamilyCommand("Gomez Family", "USER_ADMIN"), CancellationToken.None);

        created!.TenantId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task Handle_CreateFamily_WithCustomMaxMembers_ShouldPropagate()
    {
        var result = await _handler.Handle(new CreateFamilyCommand("Gomez Family", "USER_ADMIN", 12), CancellationToken.None);

        result.Data!.MaxMembers.Should().Be(12);
    }

    [Fact]
    public async Task Handle_CreateFamily_ShouldAddOnceAndPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new CreateFamilyCommand("Gomez Family", "USER_ADMIN"), cts.Token);

        _mockFamilyRepo.Verify(x => x.AddAsync(It.IsAny<Family>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateFamily_ShouldReturnUpdatedDto()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new UpdateFamilyCommand("FAM_1", "Gomez Family Renamed"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Family updated.");
        result.Data!.Name.Should().Be("Gomez Family Renamed");
        result.Data!.AdministratorUserId.Should().Be("USER_ADMIN");
    }

    [Fact]
    public async Task Handle_UpdateFamily_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new UpdateFamilyCommand("FAM_1", "Renamed"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_1*");
    }

    [Fact]
    public async Task Handle_UpdateFamily_WithEmptyName_ShouldThrowArgumentException()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var act = async () => await _handler.Handle(new UpdateFamilyCommand("FAM_1", "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Family name cannot be empty.");
    }

    [Fact]
    public async Task Handle_UpdateFamily_ShouldPersistModifiedEntity()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);
        Family? updated = null;
        _mockFamilyRepo.Setup(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
            .Callback<Family, CancellationToken>((f, _) => updated = f)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new UpdateFamilyCommand("FAM_1", "Renamed"), CancellationToken.None);

        updated.Should().BeSameAs(family);
        updated!.Name.Should().Be("Renamed");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UpdateFamily_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateFamily());

        await _handler.Handle(new UpdateFamilyCommand("FAM_1", "Renamed"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockFamilyRepo.Verify(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteFamily_ShouldReturnTrueAndSoftDelete()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateFamily());

        var result = await _handler.Handle(new DeleteFamilyCommand("FAM_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Family dissolved/deleted.");
        result.Data.Should().BeTrue();
        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync("FAM_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteFamily_ShouldPropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", cts.Token)).ReturnsAsync(CreateFamily());

        await _handler.Handle(new DeleteFamilyCommand("FAM_1"), cts.Token);

        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync("FAM_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteFamily_Twice_ShouldBeIdempotent()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateFamily());

        await _handler.Handle(new DeleteFamilyCommand("FAM_1"), CancellationToken.None);
        var second = await _handler.Handle(new DeleteFamilyCommand("FAM_1"), CancellationToken.None);

        second.Data.Should().BeTrue();
        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync("FAM_1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DeleteFamily_WithUnknownId_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new DeleteFamilyCommand("FAM_UNKNOWN"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_UNKNOWN*");
        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeleteFamily_ShouldSoftDeleteOnlyRequestedFamily()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_9", It.IsAny<CancellationToken>())).ReturnsAsync(CreateFamily("FAM_9"));

        await _handler.Handle(new DeleteFamilyCommand("FAM_9"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync("FAM_9", It.IsAny<CancellationToken>()), Times.Once);
        _mockFamilyRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddFamilyMember_ShouldReturnTrueAndAddMember()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new AddFamilyMemberCommand("FAM_1", "USER_3"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Member added to family.");
        result.Data.Should().BeTrue();
        family.MemberUserIds.Should().Contain("USER_3");
    }

    [Fact]
    public async Task Handle_AddFamilyMember_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new AddFamilyMemberCommand("FAM_1", "USER_3"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_1*");
    }

    [Fact]
    public async Task Handle_AddFamilyMember_WhenLimitReached_ShouldThrowInvalidOperationException()
    {
        var family = new Family("Tiny Family", "USER_ADMIN", "TENANT_TEST", 1);
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var act = async () => await _handler.Handle(new AddFamilyMemberCommand("FAM_1", "USER_3"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Family member limit of 1 reached.");
    }

    [Fact]
    public async Task Handle_AddFamilyMember_WhenAlreadyMember_ShouldThrowInvalidOperationException()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var act = async () => await _handler.Handle(new AddFamilyMemberCommand("FAM_1", "USER_2"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User is already a member of this family.");
    }

    [Fact]
    public async Task Handle_AddFamilyMember_ShouldPersistModifiedEntity()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        await _handler.Handle(new AddFamilyMemberCommand("FAM_1", "USER_3"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveFamilyMember_ShouldReturnTrueAndRemoveMember()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new RemoveFamilyMemberCommand("FAM_1", "USER_2"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Member removed from family.");
        result.Data.Should().BeTrue();
        family.MemberUserIds.Should().NotContain("USER_2");
    }

    [Fact]
    public async Task Handle_RemoveFamilyMember_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new RemoveFamilyMemberCommand("FAM_1", "USER_2"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_1*");
    }

    [Fact]
    public async Task Handle_RemoveFamilyMember_WhenRemovingAdministrator_ShouldThrowInvalidOperationException()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var act = async () => await _handler.Handle(new RemoveFamilyMemberCommand("FAM_1", "USER_ADMIN"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot remove the family administrator. Transfer admin role first.");
    }

    [Fact]
    public async Task Handle_RemoveFamilyMember_WhenNotMember_ShouldSucceedWithoutChanges()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new RemoveFamilyMemberCommand("FAM_1", "USER_UNKNOWN"), CancellationToken.None);

        result.Data.Should().BeTrue();
        family.MemberUserIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_RemoveFamilyMember_ShouldPersistModifiedEntity()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        await _handler.Handle(new RemoveFamilyMemberCommand("FAM_1", "USER_2"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TransferFamilyAdmin_ShouldReturnTrueAndTransferRole()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new TransferFamilyAdminCommand("FAM_1", "USER_2"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Family administrator transferred.");
        result.Data.Should().BeTrue();
        family.AdministratorUserId.Should().Be("USER_2");
    }

    [Fact]
    public async Task Handle_TransferFamilyAdmin_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new TransferFamilyAdminCommand("FAM_1", "USER_2"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_1*");
    }

    [Fact]
    public async Task Handle_TransferFamilyAdmin_WhenNewAdminNotMember_ShouldThrowInvalidOperationException()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var act = async () => await _handler.Handle(new TransferFamilyAdminCommand("FAM_1", "USER_STRANGER"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("New administrator must be a member of the family.");
    }

    [Fact]
    public async Task Handle_TransferFamilyAdmin_ShouldPersistModifiedEntity()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        await _handler.Handle(new TransferFamilyAdminCommand("FAM_1", "USER_2"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TransferFamilyAdmin_ShouldLookupAndUpdateExactlyOnce()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateFamily());

        await _handler.Handle(new TransferFamilyAdminCommand("FAM_1", "USER_2"), CancellationToken.None);

        _mockFamilyRepo.Verify(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>()), Times.Once);
        _mockFamilyRepo.Verify(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
