namespace LifeBalance.Dashboard.Infrastructure.Options;

/// <summary>
/// Configuration options for JWT Authentication.
/// Bound from <c>appsettings.json → Jwt</c>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets or sets the token issuer (iss claim).</summary>
    public string Issuer { get; set; } = "LifeBalance";

    /// <summary>Gets or sets the token audience (aud claim).</summary>
    public string Audience { get; set; } = "LifeBalance";

    /// <summary>Gets or sets the HMAC-SHA256 secret key (minimum 32 characters).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the token expiry in minutes. Defaults to 60.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
