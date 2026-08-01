using Auth.Application.Interfaces.Services;
using Auth.Shared.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            var apiKey = _smtpSettings.Password; // Reutilizamos el campo Password para la API Key de SendGrid
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("SendGrid API Key is missing. Email to {Email} was not sent.", toEmail);
                return;
            }

            var client = new SendGrid.SendGridClient(apiKey);
            var from = new SendGrid.Helpers.Mail.EmailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName);
            var to = new SendGrid.Helpers.Mail.EmailAddress(toEmail);
            var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, body, body);
            
            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email} with subject {Subject}", toEmail, subject);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid failed to send email to {Email}. Status: {StatusCode}. Details: {Details}", 
                    toEmail, response.StatusCode, errorBody);
                throw new Exception($"Failed to send email via SendGrid. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email to {Email} with subject {Subject}", toEmail, subject);
            throw;
        }
    }
}
