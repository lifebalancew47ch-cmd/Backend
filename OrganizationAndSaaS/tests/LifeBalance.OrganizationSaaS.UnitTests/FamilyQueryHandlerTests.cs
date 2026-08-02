using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.Families;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class FamilyQueryHandlerTests
{
    private readonly Mock<IRepository<Family>> _mockFamilyRepo;
    private readonly FamilyQueryHandler _handler;

    public FamilyQueryHandlerTests()
    {
        _mockFamilyRepo = new Mock<IRepository<Family>>();
        _handler = new FamilyQueryHandler(_mockFamilyRepo.Object);
    }

    private static Family CreateFamily(string id = "FAM_1", string name = "Gomez Family")
    {
        var family = new Family(name, "USER_ADMIN", "TENANT_TEST", 6);
        family.AddMember("USER_2");
        family.AddMember("USER_3");
        return family;
    }

    [Fact]
    public async Task Handle_GetFamilyById_ShouldReturnMappedDto()
    {
        var family = CreateFamily();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new GetFamilyByIdQuery("FAM_1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(family.Id);
        result.Data!.TenantId.Should().Be("TENANT_TEST");
        result.Data!.Name.Should().Be("Gomez Family");
        result.Data!.AdministratorUserId.Should().Be("USER_ADMIN");
        result.Data!.MaxMembers.Should().Be(6);
        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_ADMIN", "USER_2", "USER_3" });
        result.Data!.CreatedAt.Should().Be(family.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetFamilyById_WhenNotFound_ShouldThrowResourceNotFoundException()
    {
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var act = async () => await _handler.Handle(new GetFamilyByIdQuery("FAM_1"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>().WithMessage("*Family*FAM_1*");
    }

    [Fact]
    public async Task Handle_GetFamilyById_ShouldLookupByIdentifierAndToken()
    {
        using var cts = new CancellationTokenSource();
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", cts.Token)).ReturnsAsync(CreateFamily());

        await _handler.Handle(new GetFamilyByIdQuery("FAM_1"), cts.Token);

        _mockFamilyRepo.Verify(x => x.GetByIdAsync("FAM_1", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_GetFamilyById_ShouldPropagateMemberList()
    {
        var family = new Family("Solo Family", "USER_ADMIN", "TENANT_TEST", 2);
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new GetFamilyByIdQuery("FAM_1"), CancellationToken.None);

        result.Data!.MemberUserIds.Should().BeEquivalentTo(new[] { "USER_ADMIN" });
    }

    [Fact]
    public async Task Handle_GetFamilyById_ShouldPropagateCustomMaxMembers()
    {
        var family = new Family("Big Family", "USER_ADMIN", "TENANT_TEST", 20);
        _mockFamilyRepo.Setup(x => x.GetByIdAsync("FAM_1", It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await _handler.Handle(new GetFamilyByIdQuery("FAM_1"), CancellationToken.None);

        result.Data!.MaxMembers.Should().Be(20);
        result.Data!.Name.Should().Be("Big Family");
    }

    [Fact]
    public async Task Handle_GetFamiliesPaged_ShouldReturnMappedItemsAndTotal()
    {
        var items = new List<Family> { CreateFamily("FAM_1", "Gomez"), CreateFamily("FAM_2", "Addams") };
        _mockFamilyRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Family, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Family, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Family>, long))(items, 15L));

        var result = await _handler.Handle(new GetFamiliesPagedQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.TotalCount.Should().Be(15);
        result.Data!.PageIndex.Should().Be(1);
        result.Data!.PageSize.Should().Be(10);
        result.Data!.Items.Last().Name.Should().Be("Addams");
    }

    [Fact]
    public async Task Handle_GetFamiliesPaged_ShouldMatchAllFamilies()
    {
        Expression<Func<Family, bool>>? capturedPredicate = null;
        _mockFamilyRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Family, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Family, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Family, bool>>, int, int, Expression<Func<Family, object>>, bool, CancellationToken>((p, _, _, _, _, _) => capturedPredicate = p)
            .ReturnsAsync(((IEnumerable<Family>, long))(new List<Family>(), 0L));

        await _handler.Handle(new GetFamiliesPagedQuery(1, 10), CancellationToken.None);

        capturedPredicate!.Compile()(new Family("A", "ADMIN", "T")).Should().BeTrue();
        capturedPredicate!.Compile()(new Family("B", "ADMIN", "T")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetFamiliesPaged_ShouldOrderByCreatedAtDescending()
    {
        Expression<Func<Family, object>>? capturedOrderBy = null;
        bool capturedDescending = false;
        _mockFamilyRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Family, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Family, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Family, bool>>, int, int, Expression<Func<Family, object>>, bool, CancellationToken>((_, _, _, o, sd, _) => { capturedOrderBy = o; capturedDescending = sd; })
            .ReturnsAsync(((IEnumerable<Family>, long))(new List<Family>(), 0L));

        await _handler.Handle(new GetFamiliesPagedQuery(1, 10), CancellationToken.None);

        capturedDescending.Should().BeTrue();
        capturedOrderBy.Should().NotBeNull();
        var family = new Family("Test", "ADMIN", "T");
        capturedOrderBy!.Compile()(family).Should().Be(family.CreatedAt);
    }

    [Fact]
    public async Task Handle_GetFamiliesPaged_ShouldComputePagingProperties()
    {
        _mockFamilyRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Family, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Family, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Family>, long))(new List<Family>(), 25L));

        var result = await _handler.Handle(new GetFamiliesPagedQuery(3, 10), CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3);
        result.Data!.HasPreviousPage.Should().BeTrue();
        result.Data!.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GetFamiliesPaged_WithNoResults_ShouldReturnEmptyPage()
    {
        _mockFamilyRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<Expression<Func<Family, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Family, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Family>, long))(new List<Family>(), 0L));

        var result = await _handler.Handle(new GetFamiliesPagedQuery(1, 10), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data!.TotalCount.Should().Be(0);
        result.Data!.TotalPages.Should().Be(0);
    }
}
