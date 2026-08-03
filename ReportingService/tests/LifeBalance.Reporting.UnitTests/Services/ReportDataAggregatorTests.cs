using FluentAssertions;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Application.Services;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.ValueObjects;
using NSubstitute;

namespace LifeBalance.Reporting.UnitTests.Services;

public class ReportDataAggregatorTests
{
    private readonly IAuthServiceClient _authClient = Substitute.For<IAuthServiceClient>();
    private readonly IMedicalDataServiceClient _medicalClient = Substitute.For<IMedicalDataServiceClient>();
    private readonly IOrganizationServiceClient _organizationClient = Substitute.For<IOrganizationServiceClient>();
    private readonly ReportDataAggregator _aggregator;
    private readonly DateRange _range;

    public ReportDataAggregatorTests()
    {
        _aggregator = new ReportDataAggregator(_authClient, _medicalClient, _organizationClient);
        _range = new DateRange(
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task BuildAsync_Individual_UsesRequesterId()
    {
        var profile = new AuthUserProfileDto("user-1", "a@b.io", "A", "B", ["USER"], null, null);
        _authClient.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);
        _medicalClient.GetUserReadingsAsync("user-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _aggregator.BuildAsync(ReportScope.Individual, "attacker-id", "user-1", ["USER"], _range, CancellationToken.None);

        result.ScopeId.Should().Be("user-1");
    }

    [Fact]
    public async Task BuildAsync_Individual_MissingProfile_ThrowsUpstreamUnavailable()
    {
        _authClient.GetUserProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AuthUserProfileDto?)null);

        await FluentActions.Awaiting(() => _aggregator.BuildAsync(ReportScope.Individual, null, "user-1", ["USER"], _range, CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task BuildAsync_Family_Member_IsAllowed()
    {
        var family = new FamilyMembershipDto("fam-1", "admin-1", ["admin-1", "user-1"]);
        _organizationClient.GetFamilyAsync("fam-1", Arg.Any<CancellationToken>()).Returns(family);
        _authClient.GetFamilyMembersAsync("fam-1", Arg.Any<CancellationToken>())
            .Returns([new AuthUserProfileDto("user-1", "a@b.io", "A", "B", ["USER"], "fam-1", null)]);
        _medicalClient.GetFamilyReadingsAsync("fam-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _aggregator.BuildAsync(ReportScope.Family, "fam-1", "user-1", ["USER"], _range, CancellationToken.None);

        result.ScopeId.Should().Be("fam-1");
        result.Family.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildAsync_Family_NonMember_ThrowsAccessDenied()
    {
        var family = new FamilyMembershipDto("fam-1", "admin-1", ["admin-1", "user-2"]);
        _organizationClient.GetFamilyAsync("fam-1", Arg.Any<CancellationToken>()).Returns(family);

        await FluentActions.Awaiting(() => _aggregator.BuildAsync(ReportScope.Family, "fam-1", "user-1", ["USER"], _range, CancellationToken.None))
            .Should().ThrowAsync<ReportAccessDeniedException>();
    }

    [Fact]
    public async Task BuildAsync_Family_Admin_IsAllowed()
    {
        var family = new FamilyMembershipDto("fam-1", "admin-1", ["user-2"]);
        _organizationClient.GetFamilyAsync("fam-1", Arg.Any<CancellationToken>()).Returns(family);
        _authClient.GetFamilyMembersAsync("fam-1", Arg.Any<CancellationToken>())
            .Returns([new AuthUserProfileDto("user-2", "b@b.io", "C", "D", ["USER"], "fam-1", null)]);
        _medicalClient.GetFamilyReadingsAsync("fam-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _aggregator.BuildAsync(ReportScope.Family, "fam-1", "admin-9", ["ADMIN"], _range, CancellationToken.None);

        result.ScopeId.Should().Be("fam-1");
        result.Family.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildAsync_Company_Member_IsAllowed()
    {
        var company = new CompanyDto("comp-1", "ACME", "Health", 100, "Enterprise", DateTime.UtcNow.AddYears(1));
        _organizationClient.GetCompanyAsync("comp-1", Arg.Any<CancellationToken>()).Returns(company);
        _organizationClient.GetDepartmentsWithMembersAsync("comp-1", Arg.Any<CancellationToken>())
            .Returns([new CompanyDepartmentMembersDto("dep-1", "Engineering", ["user-1"])]);
        _medicalClient.GetCompanyReadingsAsync("comp-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _aggregator.BuildAsync(ReportScope.Company, "comp-1", "user-1", ["USER"], _range, CancellationToken.None);

        result.ScopeId.Should().Be("comp-1");
        result.Company.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildAsync_Company_NonMember_ThrowsAccessDenied()
    {
        var company = new CompanyDto("comp-1", "ACME", "Health", 100, "Enterprise", DateTime.UtcNow.AddYears(1));
        _organizationClient.GetCompanyAsync("comp-1", Arg.Any<CancellationToken>()).Returns(company);
        _organizationClient.GetDepartmentsWithMembersAsync("comp-1", Arg.Any<CancellationToken>())
            .Returns([new CompanyDepartmentMembersDto("dep-1", "Engineering", ["user-2"])]);

        await FluentActions.Awaiting(() => _aggregator.BuildAsync(ReportScope.Company, "comp-1", "user-1", ["USER"], _range, CancellationToken.None))
            .Should().ThrowAsync<ReportAccessDeniedException>();
    }
}
