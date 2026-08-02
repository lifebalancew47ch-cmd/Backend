using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace LifeBalance.Administration.IntegrationTests.Infrastructure;

/// <summary>No-op audit recorder so integration tests don't hit MongoDB.</summary>
public class InMemoryAuditService : IAuditService
{
    public ConcurrentBag<AuditEntryDto> Entries { get; } = new();

    public Task RecordAsync(AuditEntryDto entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task RecordAsync(IEnumerable<AuditEntryDto> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries) Entries.Add(entry);
        return Task.CompletedTask;
    }
}

/// <summary>Stub monitoring service returning a fixed empty board.</summary>
public class StubServiceStatusService : IServiceStatusService
{
    public Task<IReadOnlyList<ServiceStatusSnapshot>> GetBoardAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ServiceStatusSnapshot>>(Array.Empty<ServiceStatusSnapshot>());

    public Task<ServiceStatusSnapshot> GetServiceAsync(MicroserviceName service, bool forceRefresh = false, CancellationToken cancellationToken = default)
        => Task.FromResult(new ServiceStatusSnapshot(
            service, service.ToString(), ServiceHealthStatus.Unknown, null, "Not probed", 0, null, null, DateTime.UtcNow, null));
}

/// <summary>Stub Auth client returning an empty, healthy upstream.</summary>
public class StubAuthProfileServiceClient : IAuthProfileServiceClient
{
    public Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ServiceHealthResult(true, 200, "Healthy", 10, "1.0.0"));

    public Task<object?> GetUsersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<object?>(null);

    public Task<IReadOnlyList<AuthRoleDto>?> GetRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AuthRoleDto>?>(Array.Empty<AuthRoleDto>());

    public Task<IReadOnlyList<AuthPermissionDto>?> GetPermissionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AuthPermissionDto>?>(Array.Empty<AuthPermissionDto>());

    public Task<object?> GetAdministratorsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<object?>(null);
}

/// <summary>Stub Organization client returning an empty, healthy upstream.</summary>
public class StubOrganizationServiceClient : IOrganizationServiceClient
{
    public Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ServiceHealthResult(true, 200, "Healthy", 10, "1.0.0"));

    public Task<IReadOnlyList<OrganizationInfoDto>?> GetOrganizationsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OrganizationInfoDto>?>(Array.Empty<OrganizationInfoDto>());

    public Task<IReadOnlyList<OrganizationLicenseDto>?> GetLicensesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OrganizationLicenseDto>?>(Array.Empty<OrganizationLicenseDto>());
}

/// <summary>Mints valid JWTs signed with the same key/issuer/audience the app validates.</summary>
public static class TestJwtFactory
{
    public const string Secret = "IntegrationTest_AdminSecret_2026_32plus_bytes_key!!";

    public static string CreateToken(string role, string userId = "admin-user-1")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Email, "admin@lb.app"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "LifeBalance",
            audience: "LifeBalance",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
