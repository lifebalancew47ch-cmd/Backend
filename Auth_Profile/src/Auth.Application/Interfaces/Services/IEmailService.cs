namespace Auth.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default);
    Task SendEmailConfirmationEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default);
}
