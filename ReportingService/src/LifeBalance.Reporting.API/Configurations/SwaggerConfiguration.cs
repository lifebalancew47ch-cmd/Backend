using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace LifeBalance.Reporting.API.Configurations;

/// <summary>
/// Swagger / OpenAPI configuration. Only enabled in the Development environment.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>Registers Swagger generation services.</summary>
    public static IServiceCollection AddReportingSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LifeBalance Reporting Service API",
                Version = "v1",
                Description = "Microservicio de Reportes y Analítica de la plataforma LifeBalance. Consolida métricas históricas desde los microservicios de datos y genera reportes descargables (PDF, Excel, CSV), estadísticas, tendencias y métricas de sistema.",
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

            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    /// <summary>Enables Swagger and the Swagger UI.</summary>
    public static IApplicationBuilder UseReportingSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeBalance Reporting API v1");
            options.RoutePrefix = string.Empty;
            options.DocumentTitle = "LifeBalance Reporting API Documentation";
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });

        return app;
    }
}
