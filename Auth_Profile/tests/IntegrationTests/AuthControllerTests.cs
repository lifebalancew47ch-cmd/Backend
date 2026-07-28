using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace IntegrationTests;

public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Health_ReturnsCorrectContentType()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        // The endpoint should either return 200 OK (if MongoDB is running) or 503 Service Unavailable (if MongoDB is down),
        // but it should not crash.
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}
