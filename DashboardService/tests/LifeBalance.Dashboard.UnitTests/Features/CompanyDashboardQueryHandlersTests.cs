using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.CompanyDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class CompanyDashboardQueryHandlersTests
{
    private readonly ISedentaryEngineServiceClient _sedentaryClient = Substitute.For<ISedentaryEngineServiceClient>();
    private readonly IOrganizationServiceClient _orgClient = Substitute.For<IOrganizationServiceClient>();

    private readonly CompanyDashboardQueryHandlers _handler;

    public CompanyDashboardQueryHandlersTests()
    {
        _handler = new CompanyDashboardQueryHandlers(_sedentaryClient, _orgClient);
    }

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_ReturnsSuccessfulResult()
    {
        // Arrange
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 92.0, 100, 92, new List<string>()));

        var query = new GetCompanyDashboardQuery(companyId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(companyId);
        result.Value.Adherence!.AdherencePercentage.Should().Be(92.0);
    }
}
