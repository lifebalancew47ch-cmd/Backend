namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's context.
/// Implemented in the API layer using <c>IHttpContextAccessor</c>.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Gets the authenticated user's unique identifier. Null if anonymous.</summary>
    string? UserId { get; }

    /// <summary>Gets the authenticated user's email address. Null if anonymous.</summary>
    string? Email { get; }

    /// <summary>Gets whether the current request is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the roles assigned to the current user.</summary>
    IReadOnlyList<string> Roles { get; }
}
