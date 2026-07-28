using Auth.Domain.Entities;

namespace Auth.Application.Interfaces.Repositories;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default);
}
