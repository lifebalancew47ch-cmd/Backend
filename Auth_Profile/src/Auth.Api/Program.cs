using System.Text;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Infrastructure.Middlewares;
using Auth.Api.Extensions;
using MongoDB.Driver;
using AspNetCoreRateLimit;


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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomApiVersioning();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddSwaggerConfiguration();

builder.Services.AddHealthChecks()
    .AddMongoDb(sp => sp.GetRequiredService<IMongoClient>(), name: "mongodb");

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowConfiguredOrigins");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.Services.InitializeDatabaseAsync();

app.Run();

public partial class Program { }
