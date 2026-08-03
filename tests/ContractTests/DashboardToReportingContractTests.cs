using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ContractTests;

public record ReportingSystemMetricsResponseContract(
    int TotalUsers,
    int ActiveUsersToday,
    double PlatformHealthPercentage,
    string SystemVersion);

public class DashboardToReportingContractTests
{
    [Fact]
    public async Task ReportingSystemMetrics_EnvelopeContainsData_PopulatesExpectedSchema()
    {
        // Arrange
        var expectedMetrics = new ReportingSystemMetricsResponseContract(
            TotalUsers: 10000,
            ActiveUsersToday: 3200,
            PlatformHealthPercentage: 99.9,
            SystemVersion: "v1.0.0"
        );

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.Contains("/api/v1/reports/system-metrics") &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    success = true,
                    data = expectedMetrics,
                    message = "Request processed successfully."
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://lifebalance-reporting-service.onrender.com")
        };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mocked_valid_jwt_token");

        // Act
        var response = await client.GetAsync("/api/v1/reports/system-metrics");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data.GetProperty("totalUsers").GetInt32().Should().Be(10000);
        data.GetProperty("activeUsersToday").GetInt32().Should().Be(3200);
        data.GetProperty("platformHealthPercentage").GetDouble().Should().Be(99.9);
        data.GetProperty("systemVersion").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task ReportingSystemMetrics_MissingOrInvalidJwt_Returns401UnauthorizedContract()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization == null || req.Headers.Authorization.Parameter != "valid_token"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Unauthorized access.",
                    statusCode = 401
                }))
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://lifebalance-reporting-service.onrender.com")
        };

        // Act
        var response = await client.GetAsync("/api/v1/reports/system-metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
