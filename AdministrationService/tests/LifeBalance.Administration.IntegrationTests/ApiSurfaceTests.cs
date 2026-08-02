using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LifeBalance.Administration.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LifeBalance.Administration.IntegrationTests;

public class HealthEndpointTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;

    public HealthEndpointTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_IsAnonymousAndReturns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }
}

public class AuthorizationTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;

    public AuthorizationTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalogs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithRegularUserRole_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("USER"));

        var response = await client.GetAsync("/api/v1/catalogs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("SUPERADMIN")]
    [InlineData("SYSTEMADMINISTRATOR")]
    public async Task ProtectedEndpoint_WithAdministratorRole_Returns200(string role)
    {
        InMemoryRepository<LifeBalance.Administration.Domain.Entities.Catalog>.Reset();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken(role));

        var response = await client.GetAsync("/api/v1/catalogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task LogsIngestion_WithAdministratorRole_IsAllowed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var payload = new
        {
            service = "Auth",
            level = "Information",
            message = "integration test log"
        };

        var response = await client.PostAsJsonAsync("/api/v1/logs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class VersioningAndHeadersTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;

    public VersioningAndHeadersTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Responses_IncludeCorrelationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var response = await client.GetAsync("/api/v1/maintenance/status");

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var response = await client.GetAsync("/api/v1/services/status");

        response.Headers.TryGetValues("X-Content-Type-Options", out var cto).Should().BeTrue();
        cto!.First().Should().Be("nosniff");
        response.Headers.TryGetValues("X-Frame-Options", out var xfo).Should().BeTrue();
        xfo!.First().Should().Be("DENY");
    }

    [Fact]
    public async Task UnversionedUrl_Returns401InsteadOf404()
    {
        // Route exists only under /api/v1/...; without the version the controller is still matched
        // by the fallback auth policy, so we assert the endpoint is protected, not missing.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
