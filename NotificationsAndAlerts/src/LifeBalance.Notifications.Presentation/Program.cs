using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Infrastructure.Data;
using LifeBalance.Notifications.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IPreferenceService, PreferenceService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
