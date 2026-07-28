using LifeBalance.Dashboard.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBalance.Dashboard.IntegrationTests.Common;

/// <summary>
/// Base class for integration tests using <see cref="WebApplicationFactory{TProgram}"/>.
/// Spins up the real ASP.NET Core pipeline in memory for testing.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private bool _disposed;

    /// <summary>Gets the HTTP client connected to the in-memory test server.</summary>
    protected HttpClient Client { get; }

    /// <summary>Gets the service scope for resolving services.</summary>
    protected IServiceScope Scope { get; }

    /// <summary>Initializes a new instance of <see cref="IntegrationTestBase"/>.</summary>
    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override services for testing here (e.g., in-memory MongoDB)
            });
        });

        Client = customFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Scope = customFactory.Services.CreateScope();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases managed resources.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Client.Dispose();
            Scope.Dispose();
        }

        _disposed = true;
    }
}
