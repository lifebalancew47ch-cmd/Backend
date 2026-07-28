using Auth.Application.Commands.Roles;
using FluentValidation;

namespace Auth.Application.Validators.Roles;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z]+$").WithMessage("Role name must contain only letters.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");
    }
}
