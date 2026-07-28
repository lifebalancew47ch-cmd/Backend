using Auth.Application.Commands.Permissions;
using FluentValidation;

namespace Auth.Application.Validators.Permissions;

public class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Permission name is required.")
            .MaximumLength(50).WithMessage("Permission name must not exceed 50 characters.");

        RuleFor(x => x.Request.Module)
            .NotEmpty().WithMessage("Module name is required.")
            .MaximumLength(50).WithMessage("Module name must not exceed 50 characters.");
    }
}
