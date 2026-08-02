using System.Net;
using System.Text.Json;
using FluentAssertions;
using LifeBalance.Administration.IntegrationTests.Infrastructure;
using Xunit;

namespace LifeBalance.Administration.IntegrationTests;

public class IntegrationsTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;

    public IntegrationsTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthRoles_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/integrations/auth/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/integrations/auth/roles", "data")]
    [InlineData("/api/v1/integrations/auth/permissions", "data")]
    [InlineData("/api/v1/integrations/organization", "data")]
    public async Task IntegrationsEndpoints_WithAdministrator_ReturnSuccess(string path, string dataProperty)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.TryGetProperty(dataProperty, out _).Should().BeTrue();
    }
}
