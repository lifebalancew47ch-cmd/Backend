using Auth.Application.Interfaces.Services;
using Auth.Shared.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;

        var configuredUrl = configuration["App:FrontendBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            _frontendBaseUrl = configuredUrl.TrimEnd('/');
        }
        else if (environment.IsProduction())
        {
            _frontendBaseUrl = "https://lifebalance-adv3.onrender.com";
        }
        else
        {
            _frontendBaseUrl = "http://localhost:3000";
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        var resetLink = $"{_frontendBaseUrl}/auth/reset-password?token={token}&email={Uri.EscapeDataString(toEmail)}";

        var body = $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Restablecer Contraseña - LifeBalance</title>
            </head>
            <body style="margin: 0; padding: 0; background-color: #f4f6f9; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f4f6f9; padding: 40px 10px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;">
                                <!-- Header -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, #1e1b4b 0%, #312e81 50%, #4338ca 100%); padding: 36px 32px; text-align: center;">
                                        <h1 style="color: #ffffff; margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px;">LifeBalance</h1>
                                        <p style="color: #c7d2fe; margin: 6px 0 0 0; font-size: 14px; font-weight: 400;">Tu plataforma de bienestar y salud integral</p>
                                    </td>
                                </tr>
                                <!-- Body -->
                                <tr>
                                    <td style="padding: 40px 32px;">
                                        <div style="text-align: center; margin-bottom: 24px;">
                                            <div style="display: inline-block; background-color: #e0e7ff; border-radius: 50%; padding: 16px; margin-bottom: 12px;">
                                                <span style="font-size: 32px;">🔐</span>
                                            </div>
                                            <h2 style="margin: 0; font-size: 22px; font-weight: 700; color: #0f172a;">Recuperación de Contraseña</h2>
                                        </div>
                                        <p style="font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 24px;">
                                            Hola, hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>LifeBalance</strong>. Si realizaste esta solicitud, haz clic en el siguiente botón:
                                        </p>
                                        <!-- Button CTA -->
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-bottom: 30px;">
                                            <tr>
                                                <td align="center">
                                                    <a href="{resetLink}" target="_blank" style="background: linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%); color: #ffffff; padding: 14px 32px; text-decoration: none; border-radius: 10px; font-weight: 600; font-size: 15px; display: inline-block; box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3); border: none;">Restablecer Contraseña</a>
                                                </td>
                                            </tr>
                                        </table>
                                        <!-- Security Notice Box -->
                                        <div style="background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; border-radius: 8px; margin-bottom: 28px;">
                                            <p style="margin: 0; font-size: 13px; color: #92400e; line-height: 1.5;">
                                                ⏰ <strong>Información de seguridad:</strong> Este enlace expirará en <strong>60 minutos</strong>.<br/>
                                                Si no solicitaste este cambio, puedes ignorar este mensaje de forma segura. Tu contraseña permanecerá intacta.
                                            </p>
                                        </div>
                                        <!-- Direct Link Box -->
                                        <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 16px; border-radius: 8px;">
                                            <p style="margin: 0 0 8px 0; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">¿El botón no funciona? Copia este enlace en tu navegador:</p>
                                            <p style="margin: 0; font-size: 12px; color: #4338ca; word-break: break-all; font-family: monospace;"><a href="{resetLink}" style="color: #4338ca; text-decoration: underline;">{resetLink}</a></p>
                                        </div>
                                    </td>
                                </tr>
                                <!-- Footer -->
                                <tr>
                                    <td style="background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 24px 32px; text-align: center;">
                                        <p style="margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.5;">
                                            &copy; {DateTime.UtcNow.Year} LifeBalance. Todos los derechos reservados.<br/>
                                            Este es un correo automático, por favor no respondas a este mensaje.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, "Restablecimiento de Contraseña - LifeBalance", body, cancellationToken);
    }

    public async Task SendEmailConfirmationEmailAsync(string toEmail, string token, CancellationToken cancellationToken = default)
    {
        var confirmLink = $"{_frontendBaseUrl}/auth/confirm-email?token={token}&email={Uri.EscapeDataString(toEmail)}";

        var body = $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Confirma tu Correo - LifeBalance</title>
            </head>
            <body style="margin: 0; padding: 0; background-color: #f4f6f9; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f4f6f9; padding: 40px 10px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;">
                                <!-- Header -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, #065f46 0%, #047857 50%, #10b981 100%); padding: 36px 32px; text-align: center;">
                                        <h1 style="color: #ffffff; margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px;">LifeBalance</h1>
                                        <p style="color: #a7f3d0; margin: 6px 0 0 0; font-size: 14px; font-weight: 400;">Tu plataforma de bienestar y salud integral</p>
                                    </td>
                                </tr>
                                <!-- Body -->
                                <tr>
                                    <td style="padding: 40px 32px;">
                                        <div style="text-align: center; margin-bottom: 24px;">
                                            <div style="display: inline-block; background-color: #d1fae5; border-radius: 50%; padding: 16px; margin-bottom: 12px;">
                                                <span style="font-size: 32px;">✉️</span>
                                            </div>
                                            <h2 style="margin: 0; font-size: 22px; font-weight: 700; color: #0f172a;">Confirma tu Correo Electrónico</h2>
                                        </div>
                                        <p style="font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 24px;">
                                            ¡Gracias por registrarte en <strong>LifeBalance</strong>! Para completar tu registro y activar tu cuenta, por favor confirma tu dirección de correo electrónico haciendo clic en el siguiente botón:
                                        </p>
                                        <!-- Button CTA -->
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-bottom: 30px;">
                                            <tr>
                                                <td align="center">
                                                    <a href="{confirmLink}" target="_blank" style="background: linear-gradient(135deg, #059669 0%, #10b981 100%); color: #ffffff; padding: 14px 32px; text-decoration: none; border-radius: 10px; font-weight: 600; font-size: 15px; display: inline-block; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3); border: none;">Confirmar Mi Correo</a>
                                                </td>
                                            </tr>
                                        </table>
                                        <!-- Security Notice Box -->
                                        <div style="background-color: #ecfdf5; border-left: 4px solid #10b981; padding: 16px; border-radius: 8px; margin-bottom: 28px;">
                                            <p style="margin: 0; font-size: 13px; color: #065f46; line-height: 1.5;">
                                                ⏰ <strong>Nota:</strong> Este enlace expirará en <strong>24 horas</strong>.<br/>
                                                Si no creaste una cuenta en LifeBalance, simplemente ignora este correo.
                                            </p>
                                        </div>
                                        <!-- Direct Link Box -->
                                        <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 16px; border-radius: 8px;">
                                            <p style="margin: 0 0 8px 0; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">¿El botón no funciona? Copia este enlace en tu navegador:</p>
                                            <p style="margin: 0; font-size: 12px; color: #047857; word-break: break-all; font-family: monospace;"><a href="{confirmLink}" style="color: #047857; text-decoration: underline;">{confirmLink}</a></p>
                                        </div>
                                    </td>
                                </tr>
                                <!-- Footer -->
                                <tr>
                                    <td style="background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 24px 32px; text-align: center;">
                                        <p style="margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.5;">
                                            &copy; {DateTime.UtcNow.Year} LifeBalance. Todos los derechos reservados.<br/>
                                            Este es un correo automático, por favor no respondas a este mensaje.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, "Confirmación de Correo Electrónico - LifeBalance", body, cancellationToken);
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
