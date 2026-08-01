namespace Auth.Shared.Configurations;

public class SecuritySettings
{
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int PasswordResetTokenExpirationMinutes { get; set; } = 60;
    public int EmailConfirmationTokenExpirationHours { get; set; } = 24;
}
