using Auth.Application.Interfaces.Services;
using Auth.Shared.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly string _frontendBaseUrl;

    public EmailService(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:3000";
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        var resetLink = $"{_frontendBaseUrl}/auth/reset-password?token={token}&email={Uri.EscapeDataString(toEmail)}";

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
        var confirmLink = $"{_frontendBaseUrl}/auth/confirm-email?token={token}&email={Uri.EscapeDataString(toEmail)}";

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
            client.Timeout = 10000; // 10 segundos timeout (evita colgar la API 2 minutos si el puerto/SMTP está bloqueado)

            // Puerto 465 = SSL directo | Puerto 587 = StartTls (más común con Gmail)
            var socketOptions = _smtpSettings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_smtpSettings.Username))
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, cancellationToken);

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
