using FluentAssertions;
using LifeBalance.Administration.Application.Features.Logs;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class LogFeaturesTests
{
    private readonly Mock<IRepository<SystemLog>> _repo = new();

    private LogCommandHandler CreateCommandHandler() => new(_repo.Object);
    private LogQueryHandler CreateQueryHandler() => new(_repo.Object);

    [Fact]
    public void Validator_RejectsEmptyMessage()
    {
        var validator = new LogEntryRequestValidator();
        var result = validator.Validate(new LogEntryRequest(MicroserviceName.Auth, SystemLogLevel.Error, " "));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_BulkRejectsMoreThan500Entries()
    {
        var validator = new IngestLogsCommandValidator();
        var entries = Enumerable.Range(0, 501)
            .Select(i => new LogEntryRequest(MicroserviceName.Auth, SystemLogLevel.Information, $"msg {i}"))
            .ToList();

        var result = validator.Validate(new IngestLogsCommand(entries));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Ingest_PersistsAndReturnsDto()
    {
        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new IngestLogCommand(new LogEntryRequest(MicroserviceName.Organization, SystemLogLevel.Warning, "Slow query")),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Service.Should().Be("Organization");
        result.Data.Level.Should().Be("Warning");
        _repo.Verify(r => r.AddAsync(It.IsAny<SystemLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestBulk_PersistsAllEntries()
    {
        var handler = CreateCommandHandler();
        var entries = Enumerable.Range(0, 5)
            .Select(i => new LogEntryRequest(MicroserviceName.Auth, SystemLogLevel.Information, $"msg {i}"))
            .ToList();

        var result = await handler.Handle(new IngestLogsCommand(entries), CancellationToken.None);

        result.Data.Should().Be(5);
        _repo.Verify(r => r.AddAsync(It.IsAny<SystemLog>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task Query_GetById_NotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((SystemLog?)null);

        var handler = CreateQueryHandler();
        var act = async () => await handler.Handle(new GetSystemLogByIdQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task Query_ErrorLogs_FiltersByLevel()
    {
        _repo.Setup(r => r.GetPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SystemLog, bool>>>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SystemLog, object>>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { new SystemLog(MicroserviceName.Auth, SystemLogLevel.Error, "boom") }, 1L));

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetErrorLogsQuery(1, 10), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
    }
}
