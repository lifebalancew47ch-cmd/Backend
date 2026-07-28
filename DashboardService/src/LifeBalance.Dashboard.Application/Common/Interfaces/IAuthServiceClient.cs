namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record AuthUserResponseDto(string UserId, string Email, string FirstName, string LastName, List<string> Roles, string FamilyId, string CompanyId);

public interface IAuthServiceClient
{
    Task<AuthUserResponseDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<AuthUserResponseDto>> GetFamilyMembersProfileAsync(string familyId, CancellationToken cancellationToken = default);
    Task<List<AuthUserResponseDto>> GetCompanyUsersAsync(string companyId, CancellationToken cancellationToken = default);
}
