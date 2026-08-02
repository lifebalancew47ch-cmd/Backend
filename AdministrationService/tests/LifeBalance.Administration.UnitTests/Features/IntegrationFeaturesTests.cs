using FluentAssertions;
using LifeBalance.Administration.Application.Features.Integrations;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Exceptions;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class IntegrationFeaturesTests
{
    private readonly Mock<IAuthProfileServiceClient> _auth = new();
    private readonly Mock<IOrganizationServiceClient> _organization = new();

    private IntegrationQueryHandler CreateQueryHandler() => new(_auth.Object, _organization.Object);

    [Fact]
    public async Task GetAuthRoles_ReturnsRolesFromUpstream()
    {
        var roles = new[]
        {
            new AuthRoleDto("r1", "Admin", "Platform admin", new[] { "p1", "p2" }, DateTime.UtcNow)
        };
        _auth.Setup(a => a.GetRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetAuthRolesQuery(), CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data![0].Name.Should().Be("Admin");
        result.Data[0].PermissionIds.Should().Contain("p1");
    }

    [Fact]
    public async Task GetAuthRoles_UpstreamUnavailable_ThrowsFailClosed()
    {
        _auth.Setup(a => a.GetRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<AuthRoleDto>?)null);

        var handler = CreateQueryHandler();
        var act = async () => await handler.Handle(new GetAuthRolesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task GetAuthPermissions_ReturnsPermissionsFromUpstream()
    {
        var permissions = new[]
        {
            new AuthPermissionDto("p1", "roles:read", "Read roles", "Roles", DateTime.UtcNow)
        };
        _auth.Setup(a => a.GetPermissionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(permissions);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetAuthPermissionsQuery(), CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data![0].Module.Should().Be("Roles");
    }

    [Fact]
    public async Task GetAuthPermissions_UpstreamUnavailable_ThrowsFailClosed()
    {
        _auth.Setup(a => a.GetPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AuthPermissionDto>?)null);

        var handler = CreateQueryHandler();
        var act = async () => await handler.Handle(new GetAuthPermissionsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task GetOrganizationConfiguration_ReturnsOrganizationsAndLicenses()
    {
        var organizations = new[]
        {
            new OrganizationInfoDto("org1", "Acme Inc", "Active", "plan-premium", "tenant-1", DateTime.UtcNow)
        };
        var licenses = new[]
        {
            new OrganizationLicenseDto("lic1", "org1", "KEY-123", "PerSeat", "Active", null, DateTime.UtcNow, DateTime.UtcNow.AddYears(1))
        };
        _organization.Setup(o => o.GetOrganizationsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(organizations);
        _organization.Setup(o => o.GetLicensesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(licenses);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetOrganizationConfigurationQuery(), CancellationToken.None);

        result.Data!.Organizations.Should().HaveCount(1);
        result.Data.Organizations[0].Name.Should().Be("Acme Inc");
        result.Data.Licenses.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrganizationConfiguration_OrganizationsUnavailable_ThrowsFailClosed()
    {
        _organization.Setup(o => o.GetOrganizationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<OrganizationInfoDto>?)null);
        _organization.Setup(o => o.GetLicensesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new OrganizationLicenseDto("lic1", "org1", "K", "PerSeat", "Active", null, DateTime.UtcNow, DateTime.UtcNow) });

        var handler = CreateQueryHandler();
        var act = async () => await handler.Handle(new GetOrganizationConfigurationQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }
}
