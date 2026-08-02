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

public class GetProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetProfileQueryHandler>> _loggerMock = new();

    private GetProfileQueryHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        Username = "testuser",
        FirstName = "Test",
        LastName = "User",
        PhoneNumber = "123456789",
        AvatarUrl = "https://example.com/avatar.png",
        IsEmailConfirmed = true,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        LastLoginAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static UserProfileDto CreateProfileDto() =>
        new("user-123", "test@example.com", "testuser", "Test", "User", "123456789",
            "https://example.com/avatar.png", true, true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task Handle_ExistingUser_ReturnsSuccessWithMappedProfile()
    {
        // Arrange
        var user = CreateUser();
        var profileDto = CreateProfileDto();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserProfileDto>(user))
            .Returns(profileDto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetProfileQuery("user-123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().Be(profileDto);
    }

    [Fact]
    public async Task Handle_NonexistentUser_Returns404Failure()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync("ghost-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetProfileQuery("ghost-user"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsProfileWithCorrectFields()
    {
        // Arrange
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateProfileDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetProfileQuery("user-123"), CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be("user-123");
        result.Data.Email.Should().Be("test@example.com");
        result.Data.Username.Should().Be("testuser");
        result.Data.IsEmailConfirmed.Should().BeTrue();
        result.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExistingUser_CallsRepositoryWithUserId()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser());
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateProfileDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetProfileQuery("user-123"), CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_DoesNotMapProfile()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync("ghost-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetProfileQuery("ghost-user"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mapperMock.Verify(m => m.Map<UserProfileDto>(It.IsAny<User>()), Times.Never);
    }
}
