using System.Text;
using System.Threading.RateLimiting;
using LifeBalance.Notifications.Presentation.Configurations;
using LifeBalance.Notifications.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddNotificationsSwagger(builder.Environment);

var jwtOptions = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtOptions["SecretKey"];

if (builder.Environment.IsProduction() &&
    (string.IsNullOrEmpty(jwtKey) ||
     Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
     jwtKey == "CHANGE_THIS_TO_A_32_CHARACTER_SECRET_KEY_IN_PRODUCTION!!"))
{
    throw new InvalidOperationException(
        "Jwt:SecretKey debe estar configurada con al menos 32 bytes y no puede ser el valor placeholder en produccion.");
}

if (string.IsNullOrEmpty(jwtKey))
    jwtKey = "dev-only-insecure-key-for-local-development-at-least-32-bytes";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = string.IsNullOrWhiteSpace(jwtOptions["Issuer"]) ? "LifeBalance" : jwtOptions["Issuer"],
            ValidAudience = string.IsNullOrWhiteSpace(jwtOptions["Audience"]) ? "LifeBalance" : jwtOptions["Audience"],
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString()
                ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Fallback to explicit hardcoded origins in case of environment variable parsing issues on Render
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "https://lifebalance-adv3.onrender.com"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// DevSecOps: trust the Render edge proxy for the client IP and the request scheme
// (X-Forwarded-For/X-Forwarded-Proto are overwritten by Render, never by the client),
// so the IP rate limiter and UseHttpsRedirection behave correctly behind the edge.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

FirebaseInit(app.Configuration);

// Swagger / OpenAPI (Development only — Rule 7). Auditoria 6/08/2026 (S-01):
// estaba expuesto sin condicion en produccion, publicando el contrato
// completo (incluyendo endpoints admin de otros servicios) sin autenticacion.
if (app.Environment.IsDevelopment())
{
    app.UseNotificationsSwagger();
}

app.UseExceptionHandling();

app.UseSecurityHeaders();

// Restore the real client IP/scheme before HSTS/HTTPS redirect and the rate limiter.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRouting();

app.UseCors("AllowedOrigins");

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "LifeBalance.Notifications", Timestamp = DateTime.UtcNow }));

app.Run();

static void FirebaseInit(IConfiguration configuration)
{
    var options = new FirebaseAdmin.AppOptions();
    var credentialsPath = configuration["Firebase:CredentialsPath"];
    if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
    {
#pragma warning disable CS0618
        options.Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialsPath);
#pragma warning restore CS0618
    }
    else
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (!string.IsNullOrEmpty(projectId))
        {
            options.Credential = Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault();
            options.ProjectId = projectId;
        }
    }

    if (options.Credential is null)
        return;

    try { FirebaseAdmin.FirebaseApp.Create(options); } catch (InvalidOperationException) { }
}
