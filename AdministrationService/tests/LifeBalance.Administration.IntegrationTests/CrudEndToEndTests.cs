using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.IntegrationTests.Infrastructure;
using Xunit;

namespace LifeBalance.Administration.IntegrationTests;

public class CrudEndToEndTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;

    public CrudEndToEndTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateCatalog_ThenReadItBack_WorksEndToEnd()
    {
        InMemoryRepository<Catalog>.Reset();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var createPayload = new
        {
            code = "activity-type-e2e",
            name = "Activity Types",
            description = "E2E catalog",
            category = "misc",
            items = new[] { new { code = "WALK", name = "Walking", description = (string?)null, value = "5", sortOrder = 1 } }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/catalogs", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        createDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        var id = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        id.Should().NotBeNullOrWhiteSpace();

        var getResponse = await client.GetAsync($"/api/v1/catalogs/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getBody);
        getDoc.RootElement.GetProperty("data").GetProperty("code").GetString().Should().Be("ACTIVITY-TYPE-E2E");
    }

    [Fact]
    public async Task CreateCatalog_DuplicateCode_Returns409()
    {
        InMemoryRepository<Catalog>.Reset();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var payload = new { code = "dup", name = "Dup", description = "x", category = "misc" };

        var first = await client.PostAsJsonAsync("/api/v1/catalogs", payload);
        var second = await client.PostAsJsonAsync("/api/v1/catalogs", payload);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetSettings_ReturnsDefaultsWithoutMongo()
    {
        InMemoryRepository<SystemConfiguration>.Reset();
        InMemoryRepository<GlobalConfiguration>.Reset();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("data").GetProperty("globalConfig")
            .GetProperty("applicationName").GetString().Should().Be("LifeBalance");
    }

    [Fact]
    public async Task UpdateSettings_PersistsAndReturnsSuccess()
    {
        InMemoryRepository<SystemConfiguration>.Reset();
        InMemoryRepository<GlobalConfiguration>.Reset();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", TestJwtFactory.CreateToken("SUPERADMIN"));

        var payload = new
        {
            systemConfig = new
            {
                sedentary = new { maxSedentaryMinutes = 60, minActiveBreakMinutes = 5 }
            },
            globalConfig = new
            {
                applicationName = "LifeBalance PRO",
                maxUploadSizeMb = 120,
                sessionTimeoutMinutes = 30
            }
        };

        var response = await client.PutAsJsonAsync("/api/v1/settings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("data").GetProperty("systemConfig")
            .GetProperty("sedentary").GetProperty("maxSedentaryMinutes").GetInt32().Should().Be(60);
        doc.RootElement.GetProperty("data").GetProperty("globalConfig")
            .GetProperty("applicationName").GetString().Should().Be("LifeBalance PRO");
    }
}
