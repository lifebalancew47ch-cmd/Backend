namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// User profile DTO returned by the Auth &amp; Profile service.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Email">The user email.</param>
/// <param name="FirstName">The user first name.</param>
/// <param name="LastName">The user last name.</param>
/// <param name="Roles">The normalized role names.</param>
/// <param name="FamilyId">Optional family membership.</param>
/// <param name="CompanyId">Optional company membership.</param>
public sealed record AuthUserProfileDto(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    string? FamilyId,
    string? CompanyId);

/// <summary>
/// Contract for the Auth &amp; Profile microservice client.
/// All methods return <c>null</c> when the upstream call fails (fail-closed callers).
/// </summary>
public interface IAuthServiceClient
{
    /// <summary>Retrieves a user profile. Returns <c>null</c> when unavailable.</summary>
    Task<AuthUserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the profiles of all members of a family. Returns <c>null</c> when unavailable.</summary>
    Task<IReadOnlyList<AuthUserProfileDto>?> GetFamilyMembersAsync(string familyId, CancellationToken cancellationToken = default);
}
