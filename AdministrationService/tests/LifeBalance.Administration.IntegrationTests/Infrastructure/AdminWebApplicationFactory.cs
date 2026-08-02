using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LifeBalance.Administration.IntegrationTests.Infrastructure;

public class AdminWebApplicationFactory : WebApplicationFactory<Program>
{
    static AdminWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JwtSettings__Secret", TestJwtFactory.Secret);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = TestJwtFactory.Secret,
                ["ConnectionStrings:MongoDB"] = "mongodb://localhost:27017"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IAuditService>(_ => new InMemoryAuditService());
            services.AddScoped<IServiceStatusService>(_ => new StubServiceStatusService());
            services.AddScoped<IAuthProfileServiceClient>(_ => new StubAuthProfileServiceClient());
            services.AddScoped<IOrganizationServiceClient>(_ => new StubOrganizationServiceClient());
            services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));
        });
    }
}
