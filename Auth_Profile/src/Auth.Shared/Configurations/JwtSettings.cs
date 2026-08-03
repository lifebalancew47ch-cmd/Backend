namespace Auth.Shared.Configurations;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LifeBalance";
    public string Audience { get; set; } = "LifeBalance";
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
