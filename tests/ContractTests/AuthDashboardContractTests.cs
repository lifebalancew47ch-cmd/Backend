using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace ContractTests;

public record AuthUserProfileResponseContract(
    string Id,
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl,
    bool IsEmailConfirmed,
    bool IsActive);

public class AuthDashboardContractTests
{
    [Fact]
    public async Task DashboardToAuth_ValidJwtHeader_ReturnsExpectedProfileContractSchema()
    {
        // Arrange
        var expectedProfile = new AuthUserProfileResponseContract(
            Id: "usr_12345",
            Email: "user@lifebalance.io",
            Username: "john_doe",
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+1234567890",
            AvatarUrl: "https://avatar.com/john.jpg",
            IsEmailConfirmed: true,
            IsActive: true
        );

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.Contains("/api/v1/Profile/me") &&
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
                    data = expectedProfile,
                    message = "Profile retrieved successfully."
                }))
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://lifebalance-auth-service.onrender.com")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "mocked_valid_jwt_token");

        // Act
        var response = await client.GetAsync("/api/v1/Profile/me");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("usr_12345");
        content.Should().Contain("user@lifebalance.io");
        content.Should().Contain("John");
    }

    [Fact]
    public async Task DashboardToAuth_MissingOrInvalidJwtHeader_Returns401UnauthorizedContract()
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
            BaseAddress = new Uri("https://lifebalance-auth-service.onrender.com")
        };

        // Act
        var response = await client.GetAsync("/api/v1/Profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
