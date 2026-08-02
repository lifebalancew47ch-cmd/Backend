using FluentAssertions;
using LifeBalance.Administration.Application.Features.Audit;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class AuditFeaturesTests
{
    private readonly Mock<IRepository<AuditLog>> _repo = new();

    private AuditQueryHandler CreateQueryHandler() => new(_repo.Object);

    private static AuditLog CreateAuditLog() => new(
        "user-1", "user@lb.app", "CATALOG_CREATE", "Catalog", "c1",
        AuditOperationType.Create, AuditEventType.Catalog, "AdministrationService",
        "POST /api/v1/catalogs", "127.0.0.1", "agent", "corr-1", "req-1");

    [Fact]
    public async Task GetById_ReturnsMappedDto()
    {
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(CreateAuditLog());

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetAuditLogByIdQuery("1"), CancellationToken.None);

        result.Data!.Action.Should().Be("CATALOG_CREATE");
        result.Data.OperationType.Should().Be("Create");
        result.Data.EventType.Should().Be("Catalog");
    }

    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog?)null);

        var handler = CreateQueryHandler();
        var act = async () => await handler.Handle(new GetAuditLogByIdQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task GetPaged_ReturnsItems()
    {
        _repo.Setup(r => r.GetPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { CreateAuditLog() }, 1L));

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetAuditLogsPagedQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByUser_ReturnsOnlyThatUser()
    {
        _repo.Setup(r => r.GetPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { CreateAuditLog() }, 1L));

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetAuditLogsByUserQuery("user-1", 1, 10), CancellationToken.None);

        result.Data!.TotalCount.Should().Be(1);
    }
}
