using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace LifeBalance.Notifications.Presentation.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddNotificationsSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LifeBalance Notifications & Alerts Service API",
                Version = "v1",
                Description = "Microservicio de Notificaciones de LifeBalance. Gestiona el despacho centralizado de correos electrónicos, notificaciones push y alertas del sistema, incluyendo expiración de licencias y recordatorios personalizados.",
                Contact = new OpenApiContact
                {
                    Name = "Equipo LifeBalance",
                    Email = "dev@lifebalance.io"
                }
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese su token JWT Bearer: **Bearer {token}**"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseNotificationsSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeBalance Notifications & Alerts API v1");
            options.RoutePrefix = string.Empty;
            options.DocumentTitle = "LifeBalance Notifications & Alerts API Documentation";
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });

        return app;
    }
}
