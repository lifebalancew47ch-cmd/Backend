using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.FamilyDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class FamilyDashboardQueryHandlersTests
{
    private readonly IAuthServiceClient _authClient = Substitute.For<IAuthServiceClient>();
    private readonly IMedicalDataServiceClient _medicalClient = Substitute.For<IMedicalDataServiceClient>();
    private readonly IGamificationServiceClient _gamificationClient = Substitute.For<IGamificationServiceClient>();

    private readonly FamilyDashboardQueryHandlers _handler;

    public FamilyDashboardQueryHandlersTests()
    {
        _handler = new FamilyDashboardQueryHandlers(_authClient, _medicalClient, _gamificationClient);
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_ReturnsSuccessfulResult()
    {
        // Arrange
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>())
            .Returns(new List<AuthUserResponseDto>
            {
                new AuthUserResponseDto("u1", "fam1@lifebalance.io", "Alice", "Smith", new List<string>{"User"}, familyId, "c1")
            });

        var query = new GetFamilyDashboardQuery(familyId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FamilyId.Should().Be(familyId);
        result.Value.Members.Should().HaveCount(1);
    }
}
