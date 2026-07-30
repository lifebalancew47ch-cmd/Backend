using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace LifeBalance.Dashboard.API.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddDashboardSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LifeBalance Dashboard Service API",
                Version = "v1",
                Description = "Microservicio API Aggregator para la plataforma LifeBalance. Se encarga de orquestar y recopilar métricas, KPIs de salud y datos de gamificación provenientes de otros microservicios para presentarlos en los Dashboards (Individual, Familiar y Empresarial).",
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

    public static IApplicationBuilder UseDashboardSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeBalance Dashboard API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger UI at root URL /
            options.DocumentTitle = "LifeBalance Dashboard API Documentation";
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });

        return app;
    }
}
