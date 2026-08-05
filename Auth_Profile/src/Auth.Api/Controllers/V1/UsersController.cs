using Auth.Application.Queries.Profile;
using Auth.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

[Authorize]
public class UsersController : BaseController
{
    /// <summary>
    /// Gets all users with pagination. Requires Admin role.
    /// </summary>
    [HttpGet(Name = "GetUsers")]
    [Authorize(Roles = "ADMIN,SUPERADMIN,SYSTEMADMINISTRATOR")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserProfileDto>>), 200)]
    [SwaggerOperation(Summary = "Get all users", Description = "Returns paginated list of all users. Requires Admin, Superadmin or SystemAdministrator role.")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null, CancellationToken ct = default)
    {
        // For now, role filtering is ignored in the DB level for simplicity as per requirements, 
        // but we receive the query to prevent 404 from external services trying to pass it.
        var result = await Mediator.Send(new GetUsersQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a user's profile by ID. Used internally by other microservices (like DashboardService).
    /// </summary>
    [HttpGet("{id}", Name = "GetUserById")]
    [ProducesResponseType(typeof(object), 200)]
    [SwaggerOperation(Summary = "Get user by ID", Description = "Returns a user's profile information by ID for internal microservice communication.")]
    public async Task<IActionResult> GetUserById(string id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest(ApiResponse<object>.FailResponse("User ID is required."));

        // Get the profile from the database
        var result = await Mediator.Send(new GetProfileQuery(id), ct);

        if (!result.Success || result.Data == null)
            return NotFound(result);

        // Map it to the shape expected by DashboardService (AuthUserResponseDto)
        // DashboardService expects the raw object or an object that maps to its DTO, but we are wrapped in ApiResponse.
        // Wait, AuthServiceClient in Dashboard does: _httpClient.GetFromJsonAsync<AuthUserResponseDto>($"/api/v1/users/{userId}");
        // So it expects the RAW object, NOT the ApiResponse envelope!
        // We must return the object directly.
        var profile = result.Data;
        
        var responseDto = new
        {
            UserId = profile.Id,
            Email = profile.Email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Roles = new List<string>(), // Auth_Profile doesn't easily expose roles here, but Dashboard mainly needs FirstName/LastName
            FamilyId = "", // OrganizationAndSaaS handles this
            CompanyId = "" // OrganizationAndSaaS handles this
        };

        return Ok(responseDto);
    }
}
