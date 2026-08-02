using Auth.Application.Commands.Profile;
using Auth.Application.DTOs.Profile;
using Auth.Application.Handlers.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Profile;

public class UpdatePreferenceCommandHandlerTests
{
    private readonly Mock<IUserPreferenceRepository> _preferenceRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UpdatePreferenceCommandHandler>> _loggerMock = new();

    private UpdatePreferenceCommandHandler CreateHandler() => new(
        _preferenceRepositoryMock.Object,
        _auditServiceMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static UserPreferenceDto CreateDefaultDto() =>
        new("light", "en", "UTC", "metric", true, true, true, "public", false, true);

    private static UpdatePreferenceRequest CreateRequest() =>
        new("dark", "es", "Europe/Madrid", "imperial", false, true, null, "private", true, null);

    [Fact]
    public async Task Handle_ExistingPreferences_AppliesNonNullFieldsOnly()
    {
        // Arrange
        var preference = new UserPreference { UserId = "user-123" };
        var command = new UpdatePreferenceCommand(CreateRequest(), "user-123");

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        preference.Theme.Should().Be("dark");
        preference.Language.Should().Be("es");
        preference.Timezone.Should().Be("Europe/Madrid");
        preference.UnitsSystem.Should().Be("imperial");
        preference.NotificationsEnabled.Should().BeFalse();
        preference.EmailNotificationsEnabled.Should().BeTrue();
        preference.PushNotificationsEnabled.Should().BeTrue();
        preference.ProfileVisibility.Should().Be("private");
        preference.MarketingConsent.Should().BeTrue();
        preference.ActivitySharing.Should().BeTrue();
        preference.UpdatedAt.Should().NotBeNull();
        _preferenceRepositoryMock.Verify(r => r.UpdateAsync(preference, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPreferences_CreatesThenUpdates()
    {
        // Arrange
        var command = new UpdatePreferenceCommand(CreateRequest(), "user-123");

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _preferenceRepositoryMock.Verify(r => r.AddAsync(It.Is<UserPreference>(p =>
            p.UserId == "user-123"),
            It.IsAny<CancellationToken>()), Times.Once);
        _preferenceRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserPreference>(p =>
            p.UserId == "user-123" &&
            p.Theme == "dark" &&
            p.Language == "es"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPreferences_ReturnsPreferenceWithAppliedValues()
    {
        // Arrange
        var command = new UpdatePreferenceCommand(CreateRequest(), "user-123");

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _preferenceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferenceRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingPreferences_AuditsProfileUpdate()
    {
        // Arrange
        var preference = new UserPreference { UserId = "user-123" };
        var command = new UpdatePreferenceCommand(CreateRequest(), "user-123");

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.ProfileUpdate,
            "Preferences updated", null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllFieldsProvided_UpdatesEveryField()
    {
        // Arrange
        var preference = new UserPreference { UserId = "user-123" };
        var request = new UpdatePreferenceRequest("dark", "fr", "America/New_York", "imperial",
            false, false, false, "private", true, false);
        var command = new UpdatePreferenceCommand(request, "user-123");

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _preferenceRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserPreference>(p =>
            p.Theme == "dark" &&
            p.Language == "fr" &&
            p.Timezone == "America/New_York" &&
            p.UnitsSystem == "imperial" &&
            !p.NotificationsEnabled &&
            !p.EmailNotificationsEnabled &&
            !p.PushNotificationsEnabled &&
            p.ProfileVisibility == "private" &&
            p.MarketingConsent &&
            !p.ActivitySharing),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
