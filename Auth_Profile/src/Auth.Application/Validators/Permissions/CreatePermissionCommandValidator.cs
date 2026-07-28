using Auth.Application.Commands.Permissions;
using FluentValidation;

namespace Auth.Application.Validators.Permissions;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Permission name is required.")
            .MaximumLength(100).WithMessage("Permission name must not exceed 100 characters.");

        RuleFor(x => x.Request.Module)
            .NotEmpty().WithMessage("Module is required.")
            .MaximumLength(50).WithMessage("Module must not exceed 50 characters.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");
    }
}
