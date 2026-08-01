using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Validators.Auth;
using FluentValidation.TestHelper;
using Xunit;

namespace UnitTests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_ShouldHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest("", "Password123!");
        var command = new LoginCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Email)
              .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest("invalid-email-string", "Password123!");
        var command = new LoginCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Email)
              .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Validate_EmptyPassword_ShouldHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "");
        var command = new LoginCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Password)
              .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_ValidCredentials_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest("valid@example.com", "Password123!");
        var command = new LoginCommand(request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
