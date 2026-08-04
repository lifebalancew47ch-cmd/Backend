using FluentAssertions;
using LifeBalance.OrganizationSaaS.Api.Controllers.v1;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DomainLicense = LifeBalance.OrganizationSaaS.Domain.Entities.License;

namespace LifeBalance.OrganizationSaaS.UnitTests;

public class InternalMembershipControllerTests
{
    private readonly Mock<IRepository<Organization>> _orgRepo = new();
    private readonly Mock<IRepository<DomainLicense>> _licenseRepo = new();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Internal:ProvisioningKey"] = "test-internal-key"
        })
        .Build();

    private InternalMembershipController CreateController()
    {
        var controller = new InternalMembershipController(
            _orgRepo.Object,
            _licenseRepo.Object,
            _configuration,
            NullLogger<InternalMembershipController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    [Fact]
    public async Task ProvisionMembership_ValidKey_CreatesOrganizationAndAssignedLicense()
    {
        // Arrange
        var controller = CreateController();
        controller.Request.Headers["X-Internal-Key"] = "test-internal-key";

        // Act
        var result = await controller.ProvisionMembership(new ProvisionMembershipRequest("user-1"), CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);

        _orgRepo.Verify(r => r.AddAsync(It.Is<Organization>(o => !string.IsNullOrWhiteSpace(o.Id)), It.IsAny<CancellationToken>()), Times.Once);
        _licenseRepo.Verify(r => r.AddAsync(It.Is<DomainLicense>(l =>
            l.AssignedUserId == "user-1" &&
            l.Status == LicenseStatus.Assigned &&
            !string.IsNullOrWhiteSpace(l.TenantId) &&
            !string.IsNullOrWhiteSpace(l.OrganizationId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProvisionMembership_InvalidKey_ReturnsUnauthorizedAndDoesNotCreate()
    {
        // Arrange
        var controller = CreateController();
        controller.Request.Headers["X-Internal-Key"] = "wrong-key";

        // Act
        var result = await controller.ProvisionMembership(new ProvisionMembershipRequest("user-1"), CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
        _licenseRepo.Verify(r => r.AddAsync(It.IsAny<DomainLicense>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionMembership_MissingUserId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        controller.Request.Headers["X-Internal-Key"] = "test-internal-key";

        // Act
        var result = await controller.ProvisionMembership(new ProvisionMembershipRequest(" "), CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
        _licenseRepo.Verify(r => r.AddAsync(It.IsAny<DomainLicense>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
