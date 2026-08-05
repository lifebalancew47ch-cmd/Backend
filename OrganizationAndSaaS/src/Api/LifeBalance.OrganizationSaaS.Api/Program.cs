using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using LifeBalance.OrganizationSaaS.Api.Middlewares;
using LifeBalance.OrganizationSaaS.Application;
using LifeBalance.OrganizationSaaS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add Layered Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// 3. Controllers & OpenAPI / Swagger (Development only)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LifeBalance - Organization & SaaS Service API",
        Version = "v1",
        Description = "Microservicio Multi-Tenant Empresarial de LifeBalance. Administra entidades jerárquicas (Empresas, Familias, Departamentos, Equipos), suscripciones, límites de planes SaaS (Free/Pro/Business/Enterprise) e invitaciones.",
        Contact = new OpenApiContact { Name = "LifeBalance Architecture Team", Email = "architecture@lifebalance.app" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    c.EnableAnnotations();
});

// 4. Authentication & JWT Validation
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secret))
{
    throw new InvalidOperationException("JwtSettings:Secret is not configured. Set JwtSettings:Secret (at least 32 bytes) via configuration or environment variables before starting the service.");
}
if (Encoding.UTF8.GetByteCount(secret) < 32)
{
    throw new InvalidOperationException($"JwtSettings:Secret must be at least 32 bytes long (current length: {Encoding.UTF8.GetByteCount(secret)} bytes).");
}

var issuer = string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) ? "LifeBalance" : jwtSettings["Issuer"];
var audience = string.IsNullOrWhiteSpace(jwtSettings["Audience"]) ? "LifeBalance" : jwtSettings["Audience"];
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 5. Rate Limiting Setup (partitioned per remote IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 6. Response Compression (Gzip / Brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// 7. Health Checks
builder.Services.AddHealthChecks();

// 8. CORS — only configured origins; AllowAnyOrigin fallback limited to Development
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
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

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersAndCorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Organization & SaaS API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseResponseCompression();
app.UseRouting();

app.UseCors("CorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Required for Integration Tests WebApplicationFactory
public partial class Program { }
