using Auth.Application.Commands.Auth;
using FluentValidation;

namespace Auth.Application.Validators.Auth;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Request.Token)
            .NotEmpty().WithMessage("Confirmation token is required.");
    }
}
