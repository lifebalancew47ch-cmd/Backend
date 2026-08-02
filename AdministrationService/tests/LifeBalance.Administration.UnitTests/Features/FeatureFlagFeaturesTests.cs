using FluentAssertions;
using LifeBalance.Administration.Application.Features.FeatureFlags;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class FeatureFlagFeaturesTests
{
    private readonly Mock<IRepository<FeatureFlag>> _repo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private FeatureFlagCommandHandler CreateCommandHandler() => new(_repo.Object, _currentUser.Object);
    private FeatureFlagQueryHandler CreateQueryHandler() => new(_repo.Object);

    [Fact]
    public async Task Create_DefaultsEnabledByCurrentUser()
    {
        _currentUser.Setup(u => u.UserId).Returns("admin-42");
        _repo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FeatureFlag, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<FeatureFlag>());

        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new CreateFeatureFlagCommand("ai-module", "AI Module", "desc", "ai"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Code.Should().Be("AI-MODULE");
        result.Data.Status.Should().Be("Enabled");
        result.Data.EnabledBy.Should().Be("admin-42");
    }

    [Fact]
    public async Task Update_SystemFlag_ThrowsUnauthorized()
    {
        var flag = new FeatureFlag("ai", "AI", "desc", "ai", isSystem: true);
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(flag);

        var handler = CreateCommandHandler();
        var act = async () => await handler.Handle(
            new UpdateFeatureFlagCommand("1", "New", "desc", "ai"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task SetStatus_UsesCurrentUserAsActor()
    {
        _currentUser.Setup(u => u.UserId).Returns("admin-42");
        var flag = new FeatureFlag("ai", "AI", "desc", "ai");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(flag);

        var handler = CreateCommandHandler();

        await handler.Handle(new SetFeatureFlagStatusCommand("1", false), CancellationToken.None);
        flag.IsEnabled.Should().BeFalse();
        flag.DisabledBy.Should().Be("admin-42");

        await handler.Handle(new SetFeatureFlagStatusCommand("1", true), CancellationToken.None);
        flag.IsEnabled.Should().BeTrue();
        flag.EnabledBy.Should().Be("admin-42");
    }

    [Fact]
    public async Task Query_GetById_ReturnsDto()
    {
        var flag = new FeatureFlag("ai", "AI", "desc", "ai");
        flag.Enable("admin-1");
        _repo.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(flag);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetFeatureFlagByIdQuery("1"), CancellationToken.None);

        result.Data!.Status.Should().Be("Enabled");
        result.Data.EnabledBy.Should().Be("admin-1");
    }
}
