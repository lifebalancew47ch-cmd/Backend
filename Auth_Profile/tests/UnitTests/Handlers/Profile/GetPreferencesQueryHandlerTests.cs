using Auth.Application.DTOs.Profile;
using Auth.Application.Handlers.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Profile;
using Auth.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Profile;

public class GetPreferencesQueryHandlerTests
{
    private readonly Mock<IUserPreferenceRepository> _preferenceRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetPreferencesQueryHandler>> _loggerMock = new();

    private GetPreferencesQueryHandler CreateHandler() => new(
        _preferenceRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static UserPreferenceDto CreateDefaultDto() =>
        new("light", "en", "UTC", "metric", true, true, true, "public", false, true);

    [Fact]
    public async Task Handle_ExistingPreferences_ReturnsMappedPreferences()
    {
        // Arrange
        var preference = new UserPreference { UserId = "user-123", Theme = "dark" };
        var dto = new UserPreferenceDto("dark", "en", "UTC", "metric", true, true, true, "public", false, true);

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(preference))
            .Returns(dto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPreferencesQuery("user-123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(dto);
        _preferenceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoPreferences_CreatesNewPreferenceWithDefaults()
    {
        // Arrange
        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetPreferencesQuery("user-123"), CancellationToken.None);

        // Assert
        _preferenceRepositoryMock.Verify(r => r.AddAsync(It.Is<UserPreference>(p =>
            p.UserId == "user-123" &&
            p.Theme == "light" &&
            p.Language == "en" &&
            p.Timezone == "UTC" &&
            p.UnitsSystem == "metric" &&
            p.NotificationsEnabled &&
            p.EmailNotificationsEnabled &&
            p.PushNotificationsEnabled &&
            p.ProfileVisibility == "public" &&
            !p.MarketingConsent &&
            p.ActivitySharing),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPreferences_ReturnsCreatedPreference()
    {
        // Arrange
        var created = new UserPreference { UserId = "user-123" };

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.Is<UserPreference>(p => p.UserId == "user-123")))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPreferencesQuery("user-123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Theme.Should().Be("light");
        result.Data.ActivitySharing.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExistingPreferences_DoesNotCreateNew()
    {
        // Arrange
        var preference = new UserPreference { UserId = "user-123", Theme = "dark" };

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(It.IsAny<UserPreference>()))
            .Returns(CreateDefaultDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetPreferencesQuery("user-123"), CancellationToken.None);

        // Assert
        _preferenceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingPreferences_ReturnsStoredValues()
    {
        // Arrange
        var preference = new UserPreference
        {
            UserId = "user-123",
            Theme = "dark",
            Language = "es",
            Timezone = "Europe/Madrid",
            UnitsSystem = "imperial",
            NotificationsEnabled = false,
            EmailNotificationsEnabled = false,
            PushNotificationsEnabled = false,
            ProfileVisibility = "private",
            MarketingConsent = true,
            ActivitySharing = false
        };
        var dto = new UserPreferenceDto("dark", "es", "Europe/Madrid", "imperial", false, false, false, "private", true, false);

        _preferenceRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _mapperMock.Setup(m => m.Map<UserPreferenceDto>(preference))
            .Returns(dto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPreferencesQuery("user-123"), CancellationToken.None);

        // Assert
        result.Data!.Language.Should().Be("es");
        result.Data.Timezone.Should().Be("Europe/Madrid");
        result.Data.UnitsSystem.Should().Be("imperial");
        result.Data.NotificationsEnabled.Should().BeFalse();
        result.Data.ProfileVisibility.Should().Be("private");
        result.Data.MarketingConsent.Should().BeTrue();
        result.Data.ActivitySharing.Should().BeFalse();
    }
}
