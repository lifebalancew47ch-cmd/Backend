using System.Text;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Infrastructure.Middlewares;
using Auth.Api.Extensions;
using MongoDB.Driver;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

// Fail-fast JWT: never start in Production with an empty, short or placeholder secret.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (builder.Environment.IsProduction() &&
    (string.IsNullOrEmpty(jwtSecretKey) ||
     Encoding.UTF8.GetByteCount(jwtSecretKey) < 32 ||
     jwtSecretKey == "CHANGE_THIS_TO_A_32_CHARACTER_SECRET_KEY_IN_PRODUCTION!!"))
{
    throw new InvalidOperationException(
        "Jwt:SecretKey must be configured with at least 32 bytes and cannot be the placeholder value in production.");
}

// DevSecOps fail-fast: never start in Production with a missing or placeholder internal
// provisioning key (used to call the Organization service's /internal/memberships endpoint).
var provisioningKey = builder.Configuration["Internal:ProvisioningKey"];
if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(provisioningKey) ||
     provisioningKey == "CHANGE_THIS_TO_A_LONG_INTERNAL_PROVISIONING_KEY!!"))
{
    throw new InvalidOperationException(
        "Internal:ProvisioningKey must be configured and cannot be the placeholder value in production.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCustomApiVersioning();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddSwaggerConfiguration();

builder.Services.AddHealthChecks()
    .AddMongoDb(sp => sp.GetRequiredService<IMongoClient>(), name: "mongodb");

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

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Restore the real client IP/scheme before HSTS/HTTPS redirect and the IP rate limiter.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Swagger / OpenAPI (Development only — Rule 7). Auditoria 6/08/2026 (S-01):
// estaba expuesto sin condicion en produccion — este es el servicio de
// Auth/Profile, el mas sensible de exponer sin proteger.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowConfiguredOrigins");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.Services.InitializeDatabaseAsync();

app.Run();

public partial class Program { }
