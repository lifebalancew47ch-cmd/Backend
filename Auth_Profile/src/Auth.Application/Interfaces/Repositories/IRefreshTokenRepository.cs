using Auth.Domain.Entities;

namespace Auth.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(string userId, string? revokedByIp = null, CancellationToken cancellationToken = default);
}
