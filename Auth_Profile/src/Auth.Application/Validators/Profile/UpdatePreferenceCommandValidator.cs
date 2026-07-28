using Auth.Application.Commands.Profile;
using FluentValidation;

namespace Auth.Application.Validators.Profile;

public class UpdatePreferenceCommandValidator : AbstractValidator<UpdatePreferenceCommand>
{
    public UpdatePreferenceCommandValidator()
    {
        RuleFor(x => x.Request.Theme)
            .Must(t => t == null || new[] { "light", "dark", "system" }.Contains(t.ToLowerInvariant()))
            .WithMessage("Theme must be 'light', 'dark', or 'system'.");

        RuleFor(x => x.Request.Language)
            .Must(l => l == null || l.Length <= 10)
            .WithMessage("Language code must not exceed 10 characters.");

        RuleFor(x => x.Request.Timezone)
            .Must(t => t == null || t.Length <= 50)
            .WithMessage("Timezone must not exceed 50 characters.");

        RuleFor(x => x.Request.UnitsSystem)
            .Must(u => u == null || new[] { "metric", "imperial" }.Contains(u.ToLowerInvariant()))
            .WithMessage("Units system must be 'metric' or 'imperial'.");

        RuleFor(x => x.Request.ProfileVisibility)
            .Must(v => v == null || new[] { "public", "private", "friends" }.Contains(v.ToLowerInvariant()))
            .WithMessage("Profile visibility must be 'public', 'private', or 'friends'.");
    }
}
