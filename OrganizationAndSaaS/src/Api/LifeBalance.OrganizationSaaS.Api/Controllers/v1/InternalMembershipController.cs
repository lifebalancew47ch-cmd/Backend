using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

/// <summary>
/// Internal provisioning endpoints used only by the Auth service (never exposed to clients).
/// Guards cross-service calls with a shared internal key (header X-Internal-Key).
/// </summary>
[ApiController]
[Route("api/v1/internal/memberships")]
[Produces("application/json")]
public class InternalMembershipController : ControllerBase
{
    private readonly IRepository<Organization> _orgRepository;
    private readonly IRepository<License> _licenseRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalMembershipController> _logger;

    public InternalMembershipController(
        IRepository<Organization> orgRepository,
        IRepository<License> licenseRepository,
        IConfiguration configuration,
        ILogger<InternalMembershipController> logger)
    {
        _orgRepository = orgRepository;
        _licenseRepository = licenseRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Auto-provisions a tenant membership for a registered user by creating a dedicated
    /// organization (own tenant) plus a license assigned to that user. Idempotent from the
    /// caller's perspective: Auth only invokes this when the user has no tenant membership.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ProvisionMembership(
        [FromBody] ProvisionMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var expectedKey = _configuration["Internal:ProvisioningKey"];
        var providedKey = Request.Headers["X-Internal-Key"].ToString();

        if (string.IsNullOrWhiteSpace(expectedKey)
            || string.IsNullOrWhiteSpace(providedKey)
            || !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejected internal provisioning request: invalid/missing X-Internal-Key.");
            return Unauthorized(ApiResponse<ProvisionMembershipResponse>.Fail("Invalid internal provisioning key."));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(ApiResponse<ProvisionMembershipResponse>.Fail("UserId is required."));
        }

        try
        {
            var tenantId = Guid.NewGuid().ToString("N");

            var organization = new Organization("Organization", "", "", tenantId, new ContactInfo(), new Address());
            await _orgRepository.AddAsync(organization, cancellationToken);

            var license = new License(organization.Id, "Pro", DateTime.UtcNow.AddYears(1), tenantId);
            license.AssignToUser(request.UserId);
            await _licenseRepository.AddAsync(license, cancellationToken);

            _logger.LogInformation("Provisioned membership for user {UserId}: tenant {TenantId}, organization {OrganizationId}.",
                request.UserId, tenantId, organization.Id);

            return Ok(ApiResponse<ProvisionMembershipResponse>.Ok(
                new ProvisionMembershipResponse(tenantId, organization.Id),
                "Membership provisioned successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision membership for user {UserId}.", request.UserId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<ProvisionMembershipResponse>.Fail("Failed to provision membership."));
        }
    }
}

public record ProvisionMembershipRequest(string UserId);
public record ProvisionMembershipResponse(string TenantId, string OrganizationId);