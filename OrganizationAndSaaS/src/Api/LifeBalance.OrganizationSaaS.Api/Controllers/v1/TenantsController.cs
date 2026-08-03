using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/tenants")]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly IRepository<License> _licenseRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<Team> _teamRepository;
    private readonly IRepository<Family> _familyRepository;

    public TenantsController(
        IRepository<License> licenseRepository,
        IRepository<Department> departmentRepository,
        IRepository<Team> teamRepository,
        IRepository<Family> familyRepository)
    {
        _licenseRepository = licenseRepository;
        _departmentRepository = departmentRepository;
        _teamRepository = teamRepository;
        _familyRepository = familyRepository;
    }

    /// <summary>
    /// Resolves the tenant context for the authenticated user. Used by the Auth service to
    /// embed tenant_id / organization_id claims into issued JWTs. Intentionally bypasses the
    /// MediatR multi-tenant pipeline behavior so it can be called with a token that does not
    /// yet carry a tenant_id claim. The user id is always taken from the JWT (sub / NameIdentifier).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyTenant(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var licenses = await _licenseRepository.FindAsync(
            x => x.AssignedUserId == userId && x.Status == LicenseStatus.Assigned,
            cancellationToken);

        var license = licenses.FirstOrDefault();
        if (license is not null)
        {
            return Ok(ApiResponse<TenantContextResponse>.Ok(new TenantContextResponse(license.TenantId, license.OrganizationId)));
        }

        var departments = await _departmentRepository.FindAsync(
            x => x.MemberUserIds.Contains(userId),
            cancellationToken);

        var department = departments.FirstOrDefault();
        if (department is not null)
        {
            return Ok(ApiResponse<TenantContextResponse>.Ok(new TenantContextResponse(department.TenantId, department.OrganizationId)));
        }

        var teams = await _teamRepository.FindAsync(
            x => x.MemberUserIds.Contains(userId),
            cancellationToken);

        var team = teams.FirstOrDefault();
        if (team is not null)
        {
            return Ok(ApiResponse<TenantContextResponse>.Ok(new TenantContextResponse(team.TenantId, team.OrganizationId)));
        }

        var families = await _familyRepository.FindAsync(
            x => x.AdministratorUserId == userId || x.MemberUserIds.Contains(userId),
            cancellationToken);

        var family = families.FirstOrDefault();
        if (family is not null)
        {
            return Ok(ApiResponse<TenantContextResponse>.Ok(new TenantContextResponse(family.TenantId, null)));
        }

        return NotFound(ApiResponse<TenantContextResponse>.Fail("No tenant membership found for the user."));
    }
}

public record TenantContextResponse(string TenantId, string? OrganizationId);
