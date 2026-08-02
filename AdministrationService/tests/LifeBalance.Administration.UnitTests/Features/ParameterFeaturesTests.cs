using FluentAssertions;
using LifeBalance.Administration.Application.Features.Parameters;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class ParameterFeaturesTests
{
    private readonly Mock<IRepository<SystemParameter>> _repo = new();

    private ParameterCommandHandler CreateCommandHandler() => new(_repo.Object);
    private ParameterQueryHandler CreateQueryHandler() => new(_repo.Object);

    [Fact]
    public async Task Create_Succeeds()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SystemParameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SystemParameter>());

        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new CreateParameterCommand("max-score", "Max Score", "desc", ParameterDataType.Number, "100", "rules"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.DataType.Should().Be("Number");
        result.Data.Status.Should().Be("Active");
        _repo.Verify(r => r.AddAsync(It.IsAny<SystemParameter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateCode_ThrowsConflict()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SystemParameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new SystemParameter("max-score", "Max", "desc", ParameterDataType.Number, "100", "r") });

        var handler = CreateCommandHandler();
        var act = async () => await handler.Handle(
            new CreateParameterCommand("max-score", "Max", "desc", ParameterDataType.Number, "100", "r"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_SystemParameter_ThrowsUnauthorized()
    {
        var systemParameter = new SystemParameter("system-x", "X", "desc", ParameterDataType.Number, "100", "r", isSystem: true);
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(systemParameter);

        var handler = CreateCommandHandler();
        var act = async () => await handler.Handle(
            new UpdateParameterCommand("1", "New", "desc", ParameterDataType.Number, "90", "r"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task Update_NonSystem_Persists()
    {
        var parameter = new SystemParameter("max-score", "Max", "desc", ParameterDataType.Number, "100", "r");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(parameter);

        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new UpdateParameterCommand("1", "New Max", "desc", ParameterDataType.Number, "90", "r"), CancellationToken.None);

        result.Success.Should().BeTrue();
        parameter.Name.Should().Be("New Max");
        _repo.Verify(r => r.UpdateAsync(parameter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Query_GetById_ReturnsDto()
    {
        var parameter = new SystemParameter("max-score", "Max", "desc", ParameterDataType.Boolean, "true", "r");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(parameter);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetParameterByIdQuery("1"), CancellationToken.None);

        result.Data!.DataType.Should().Be("Boolean");
        result.Data.Code.Should().Be("max-score");
    }
}
