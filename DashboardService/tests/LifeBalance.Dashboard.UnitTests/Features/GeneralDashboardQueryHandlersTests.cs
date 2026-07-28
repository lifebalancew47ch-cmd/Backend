using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.GeneralDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class GeneralDashboardQueryHandlersTests
{
    private readonly IReportingServiceClient _reportingClient = Substitute.For<IReportingServiceClient>();
    private readonly GeneralDashboardQueryHandlers _handler;

    public GeneralDashboardQueryHandlersTests()
    {
        _handler = new GeneralDashboardQueryHandlers(_reportingClient);
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReturnsSummary()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(5000, 1500, 99.9, "1.0.0"));

        var query = new GetGeneralSummaryQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveUsers.Should().Be(1500);
        result.Value.GlobalHealthScore.Should().Be(99.9);
    }
}
