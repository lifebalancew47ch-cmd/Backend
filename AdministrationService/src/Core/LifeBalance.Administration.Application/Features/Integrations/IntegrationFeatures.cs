using MediatR;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Exceptions;

namespace LifeBalance.Administration.Application.Features.Integrations;

public record OrganizationConfigurationDto(
    IReadOnlyList<OrganizationInfoDto> Organizations,
    IReadOnlyList<OrganizationLicenseDto> Licenses);

public record GetAuthRolesQuery : IRequest<ApiResponse<IReadOnlyList<AuthRoleDto>>>;

public record GetAuthPermissionsQuery : IRequest<ApiResponse<IReadOnlyList<AuthPermissionDto>>>;

public record GetOrganizationConfigurationQuery : IRequest<ApiResponse<OrganizationConfigurationDto>>;

/// <summary>
/// Live data fetched from the services this administration API orchestrates:
/// Auth &amp; Profile (roles / permissions) and Organization &amp; SaaS (organizational
/// configuration). Handlers are fail-closed: when the upstream client returns
/// null the request becomes a 503 instead of silently degrading.
/// </summary>
public class IntegrationQueryHandler :
    IRequestHandler<GetAuthRolesQuery, ApiResponse<IReadOnlyList<AuthRoleDto>>>,
    IRequestHandler<GetAuthPermissionsQuery, ApiResponse<IReadOnlyList<AuthPermissionDto>>>,
    IRequestHandler<GetOrganizationConfigurationQuery, ApiResponse<OrganizationConfigurationDto>>
{
    private const string AuthServiceName = "Auth & Profile";
    private const string OrganizationServiceName = "Organization & SaaS";

    private readonly IAuthProfileServiceClient _auth;
    private readonly IOrganizationServiceClient _organization;

    public IntegrationQueryHandler(
        IAuthProfileServiceClient auth,
        IOrganizationServiceClient organization)
    {
        _auth = auth;
        _organization = organization;
    }

    public async Task<ApiResponse<IReadOnlyList<AuthRoleDto>>> Handle(GetAuthRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _auth.GetRolesAsync(cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(AuthServiceName);

        return ApiResponse<IReadOnlyList<AuthRoleDto>>.Ok(roles, "Roles retrieved from Auth & Profile.");
    }

    public async Task<ApiResponse<IReadOnlyList<AuthPermissionDto>>> Handle(GetAuthPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _auth.GetPermissionsAsync(cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(AuthServiceName);

        return ApiResponse<IReadOnlyList<AuthPermissionDto>>.Ok(permissions, "Permissions retrieved from Auth & Profile.");
    }

    public async Task<ApiResponse<OrganizationConfigurationDto>> Handle(GetOrganizationConfigurationQuery request, CancellationToken cancellationToken)
    {
        var organizations = await _organization.GetOrganizationsAsync(cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(OrganizationServiceName);

        var licenses = await _organization.GetLicensesAsync(cancellationToken) ?? Array.Empty<OrganizationLicenseDto>();

        return ApiResponse<OrganizationConfigurationDto>.Ok(
            new OrganizationConfigurationDto(organizations, licenses),
            "Organizational configuration retrieved from Organization & SaaS.");
    }
}
