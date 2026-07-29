using LifeBalance.Notifications.Presentation.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

FirebaseInit(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void FirebaseInit(IConfiguration configuration)
{
    var options = new FirebaseAdmin.AppOptions();
    var credentialsPath = configuration["Firebase:CredentialsPath"];
    if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
        options.Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialsPath);
    else
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (!string.IsNullOrEmpty(projectId))
        {
            options.Credential = Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault();
            options.ProjectId = projectId;
        }
    }
    try { FirebaseAdmin.FirebaseApp.Create(options); } catch (InvalidOperationException) { }
}
