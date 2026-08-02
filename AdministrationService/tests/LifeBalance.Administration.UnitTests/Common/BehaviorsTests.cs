using FluentAssertions;
using FluentValidation;
using LifeBalance.Administration.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeBalance.Administration.UnitTests.Common;

public class ValidationBehaviorTests
{
    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator() => RuleFor(x => x.Value).NotEmpty();
    }

    [Fact]
    public async Task Handle_ValidRequest_InvokesNext()
    {
        var validators = new IValidator<TestRequest>[] { new TestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var nextCalled = false;

        var result = await behavior.Handle(new TestRequest("ok"), () =>
        {
            nextCalled = true;
            return Task.FromResult("done");
        }, CancellationToken.None);

        result.Should().Be("done");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsWithoutInvokingNext()
    {
        var validators = new IValidator<TestRequest>[] { new TestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var nextCalled = false;

        var act = async () => await behavior.Handle(new TestRequest(""), () =>
        {
            nextCalled = true;
            return Task.FromResult("done");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoValidators_InvokesNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());

        var result = await behavior.Handle(new TestRequest("ok"), () => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
    }
}

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_LogsAndPassesThrough()
    {
        var behavior = new LoggingBehavior<Ping, string>(NullLogger<LoggingBehavior<Ping, string>>.Instance);

        var result = await behavior.Handle(new Ping(), () => Task.FromResult("pong"), CancellationToken.None);

        result.Should().Be("pong");
    }

    private sealed record Ping : IRequest<string>;
}

public class PerformanceBehaviorTests
{
    [Fact]
    public async Task Handle_FastRequest_PassesThrough()
    {
        var behavior = new PerformanceBehavior<Ping, string>(NullLogger<PerformanceBehavior<Ping, string>>.Instance);

        var result = await behavior.Handle(new Ping(), () => Task.FromResult("pong"), CancellationToken.None);

        result.Should().Be("pong");
    }

    private sealed record Ping : IRequest<string>;
}
