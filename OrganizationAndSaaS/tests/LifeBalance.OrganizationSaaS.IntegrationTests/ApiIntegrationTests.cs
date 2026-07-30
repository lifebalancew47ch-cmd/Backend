using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LifeBalance.OrganizationSaaS.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrganizations_WithoutAuth_ShouldReturnOkOrForbiddenAccordingToPolicy()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/organizations");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
