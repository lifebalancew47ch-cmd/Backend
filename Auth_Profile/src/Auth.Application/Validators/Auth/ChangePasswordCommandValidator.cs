using Auth.Application.Commands.Auth;
using FluentValidation;

namespace Auth.Application.Validators.Auth;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Request.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(12).WithMessage("New password must be at least 12 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("New password must contain at least one digit.")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage("New password must contain at least one special character.")
            .NotEqual(x => x.Request.CurrentPassword).WithMessage("New password must be different from current password.");

        RuleFor(x => x.Request.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.Request.NewPassword).WithMessage("Passwords do not match.");
    }
}
