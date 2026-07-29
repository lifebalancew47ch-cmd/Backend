namespace LifeBalance.Notifications.Infrastructure.Configuration;

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "lifebalancew47ch@gmail.com";
    public string FromName { get; set; } = "LifeBalance";
}
