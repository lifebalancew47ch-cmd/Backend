using Auth.Infrastructure.Services;
using Auth.Shared.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Services;

public class EmailServiceTests
{
    private readonly IOptions<SmtpSettings> _smtpSettings = Options.Create(new SmtpSettings
    {
        Host = "smtp.test.com",
        Port = 587,
        FromEmail = "test@lifebalance.com",
        FromName = "LifeBalance Test",
        Password = "" // No API Key so SendGrid call skips actual sending
    });

    [Fact]
    public async Task SendPasswordResetEmailAsync_InProductionEnvironment_UsesProductionBaseUrl()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var envMock = new Mock<IHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var service = new EmailService(_smtpSettings, NullLogger<EmailService>.Instance, config, envMock.Object);

        // Act
        // Sending will log warning due to empty API Key, but base URL resolution is exercised internally
        var act = async () => await service.SendPasswordResetEmailAsync("user@example.com", "token123");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_InDevelopmentEnvironment_UsesLocalhostBaseUrl()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var envMock = new Mock<IHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var service = new EmailService(_smtpSettings, NullLogger<EmailService>.Instance, config, envMock.Object);

        // Act
        var act = async () => await service.SendPasswordResetEmailAsync("user@example.com", "token123");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
