using FluentAssertions;
using Moq;
using LifeBalance.OrganizationSaaS.Application.Features.Organizations;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;
using Xunit;

namespace LifeBalance.OrganizationSaaS.UnitTests.Application;

public class OrganizationCommandHandlerTests
{
    private readonly Mock<IRepository<Organization>> _mockOrgRepo;
    private readonly Mock<IRepository<SaaSPlan>> _mockPlanRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly OrganizationCommandHandler _handler;

    public OrganizationCommandHandlerTests()
    {
        _mockOrgRepo = new Mock<IRepository<Organization>>();
        _mockPlanRepo = new Mock<IRepository<SaaSPlan>>();
        _mockTenantContext = new Mock<ITenantContext>();

        _mockTenantContext.Setup(x => x.TenantId).Returns("TENANT_TEST");

        _handler = new OrganizationCommandHandler(_mockOrgRepo.Object, _mockPlanRepo.Object, _mockTenantContext.Object);
    }

    [Fact]
    public async Task Handle_CreateOrganization_ShouldReturnSuccessResponse()
    {
        // Arrange
        var command = new CreateOrganizationCommand("Initech Corp", "TAX999", "PLAN_BUSINESS", new ContactInfo(), new Address());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Initech Corp");
        _mockOrgRepo.Verify(x => x.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
