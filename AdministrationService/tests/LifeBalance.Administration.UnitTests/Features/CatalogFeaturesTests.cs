using FluentAssertions;
using LifeBalance.Administration.Application.Features.Catalogs;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class CatalogFeaturesTests
{
    private readonly Mock<IRepository<Catalog>> _repo = new();

    private CatalogCommandHandler CreateCommandHandler() => new(_repo.Object);
    private CatalogQueryHandler CreateQueryHandler() => new(_repo.Object);

    [Fact]
    public async Task Create_SucceedsAndUppercasesCode()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Catalog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Catalog>());

        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new CreateCatalogCommand("activity-type", "Activity Types", "desc", "misc"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Code.Should().Be("ACTIVITY-TYPE");
        result.Data.Status.Should().Be("Active");
        _repo.Verify(r => r.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateCode_ThrowsConflict()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Catalog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Catalog("activity-type", "Activity Types", "desc", "misc") });

        var handler = CreateCommandHandler();
        var act = async () => await handler.Handle(
            new CreateCatalogCommand("activity-type", "Activity Types", "desc", "misc"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_NotFound_ThrowsResourceNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Catalog?)null);

        var handler = CreateCommandHandler();
        var act = async () => await handler.Handle(
            new UpdateCatalogCommand("missing", "name", "desc", "cat"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Delete_SoftDeletesEntity()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(catalog);

        var handler = CreateCommandHandler();
        var result = await handler.Handle(new DeleteCatalogCommand("1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        _repo.Verify(r => r.SoftDeleteAsync("1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStatus_ActivatesAndDeactivates()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(catalog);

        var handler = CreateCommandHandler();

        await handler.Handle(new SetCatalogStatusCommand("1", false), CancellationToken.None);
        catalog.IsActive.Should().BeFalse();

        await handler.Handle(new SetCatalogStatusCommand("1", true), CancellationToken.None);
        catalog.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Query_GetById_ReturnsDto()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(catalog);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetCatalogByIdQuery("1"), CancellationToken.None);

        result.Data!.Code.Should().Be("CODE");
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_Paged_AppliesSearchAndPaging()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");
        _repo.Setup(r => r.GetPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Catalog, bool>>>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Catalog, object>>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { catalog }, 1L));

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetCatalogsPagedQuery(1, 10, "na.me"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.TotalCount.Should().Be(1);
    }
}
