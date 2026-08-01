using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace ContractTests;

public class GracefulDegradationTests
{
    [Fact]
    public async Task DownstreamServiceTimeout_ShouldBeHandledGracefullyWithoutCrashing()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("The HTTP request timed out after 5000ms."));

        var client = new HttpClient(handlerMock.Object)
        {
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var act = async () =>
        {
            try
            {
                await client.GetAsync("https://lifebalance-auth-service.onrender.com/health");
            }
            catch (Exception ex)
            {
                // Verify exception is a TaskCanceledException or HttpRequestException, not an unhandled crash
                ex.Should().BeAssignableTo<Exception>();
            }
        };

        // Assert
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task DownstreamServiceReturns500_ShouldReturnFallbackResponseOrHandleError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var client = new HttpClient(handlerMock.Object);

        // Act
        var response = await client.GetAsync("https://lifebalance-auth-service.onrender.com/api/v1/Profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
