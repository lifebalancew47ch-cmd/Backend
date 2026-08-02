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

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UpdateProfileCommandHandler>> _loggerMock = new();

    private UpdateProfileCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _auditServiceMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        Username = "testuser",
        FirstName = "Old First",
        LastName = "Old Last",
        PhoneNumber = "111111111",
        AvatarUrl = "https://example.com/old.png"
    };

    private static UserProfileDto CreateProfileDto() =>
        new("user-123", "test@example.com", "testuser", "New First", "New Last", "222222222",
            "https://example.com/new.png", false, true, DateTime.UtcNow, null);

    private static UpdateProfileRequest CreateRequest(string? phoneNumber = "222222222", string? avatarUrl = "https://example.com/new.png") =>
        new("New First", "New Last", phoneNumber, avatarUrl);

    [Fact]
    public async Task Handle_ExistingUser_UpdatesProfileAndReturnsSuccess()
    {
        // Arrange
        var user = CreateUser();
        var command = new UpdateProfileCommand(CreateRequest(), "user-123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateProfileDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.FirstName.Should().Be("New First");
        user.LastName.Should().Be("New Last");
        user.PhoneNumber.Should().Be("222222222");
        user.AvatarUrl.Should().Be("https://example.com/new.png");
        user.UpdatedAt.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_Returns404Failure()
    {
        // Arrange
        var command = new UpdateProfileCommand(CreateRequest(), "ghost-user");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("ghost-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("User not found");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequestWithNullOptionals_DoesNotOverwriteExistingValues()
    {
        // Arrange
        var user = CreateUser();
        var command = new UpdateProfileCommand(CreateRequest(phoneNumber: null, avatarUrl: null), "user-123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        user.FirstName.Should().Be("New First");
        user.LastName.Should().Be("New Last");
        user.PhoneNumber.Should().Be("111111111");
        user.AvatarUrl.Should().Be("https://example.com/old.png");
    }

    [Fact]
    public async Task Handle_ExistingUser_AuditsProfileUpdate()
    {
        // Arrange
        var user = CreateUser();
        var command = new UpdateProfileCommand(CreateRequest(), "user-123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateProfileDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.ProfileUpdate,
            "Profile updated", null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_PersistsUpdatedProfileData()
    {
        // Arrange
        var user = CreateUser();
        var command = new UpdateProfileCommand(CreateRequest(), "user-123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateProfileDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.Id == "user-123" &&
            u.FirstName == "New First" &&
            u.LastName == "New Last" &&
            u.PhoneNumber == "222222222" &&
            u.AvatarUrl == "https://example.com/new.png"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Data.Should().NotBeNull();
    }
}
