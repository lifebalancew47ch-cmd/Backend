namespace Auth.Application.Interfaces.Services;

public record TenantContextResult(string? TenantId, string? OrganizationId);

public interface IOrganizationService
{
    Task<TenantContextResult?> GetTenantContextAsync(string accessToken, CancellationToken cancellationToken = default);
}
