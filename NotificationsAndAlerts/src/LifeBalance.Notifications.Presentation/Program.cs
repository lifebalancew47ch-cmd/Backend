using System.Text;
using LifeBalance.Notifications.Presentation.Configurations;
using LifeBalance.Notifications.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddNotificationsSwagger();

var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? builder.Configuration["Jwt:SecretKey"] 
    ?? "SUPER_SECRET_KEY_FOR_LOCAL_DEVELOPMENT_THAT_IS_LONG_ENOUGH_32_CHARS";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

FirebaseInit(app.Configuration);

app.UseNotificationsSwagger();

app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
