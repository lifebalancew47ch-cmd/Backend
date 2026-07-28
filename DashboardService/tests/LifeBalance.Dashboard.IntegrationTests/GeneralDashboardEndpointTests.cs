using FluentAssertions;
using System.Net;
using Xunit;

namespace LifeBalance.Dashboard.IntegrationTests;

public class GeneralDashboardEndpointTests
{
    [Fact]
    public void Endpoint_Route_Formatting_Is_Valid()
    {
        var route = "/api/v1/dashboard/health";
        route.Should().StartWith("/api/v1/dashboard/");
    }
}
