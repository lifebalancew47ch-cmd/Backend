using Auth.Application.Interfaces.Services;
using Auth.Infrastructure.Configurations;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        var resetLink = $"http://localhost:3000/auth/reset-password?token={token}&email={Uri.EscapeDataString(toEmail)}";

        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; padding: 20px;">
                <h2>Password Reset</h2>
                <p>You have requested to reset your password. Click the link below to proceed:</p>
                <p><a href="{resetLink}" style="background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">Reset Password</a></p>
                <p>Or copy this link into your browser:</p>
                <p><a href="{resetLink}">{resetLink}</a></p>
                <p>This link will expire in 60 minutes.</p>
                <p>If you did not request this, please ignore this email.</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, "Password Reset", body, cancellationToken);
    }

    public async Task SendEmailConfirmationEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        var confirmLink = $"http://localhost:3000/auth/confirm-email?token={token}&email={Uri.EscapeDataString(toEmail)}";

        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; padding: 20px;">
                <h2>Confirm Your Email</h2>
                <p>Thank you for registering. Please confirm your email address by clicking the link below:</p>
                <p><a href="{confirmLink}" style="background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">Confirm Email</a></p>
                <p>Or copy this link into your browser:</p>
                <p><a href="{confirmLink}">{confirmLink}</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not create an account, please ignore this email.</p>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, "Confirm Your Email", body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            if (!_smtpSettings.UseSsl)
                client.Connect(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            else
                client.Connect(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.UseSsl);

            if (!string.IsNullOrEmpty(_smtpSettings.Username))
                client.Authenticate(_smtpSettings.Username, _smtpSettings.Password);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email} with subject {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", toEmail, subject);
            throw;
        }
    }
}
