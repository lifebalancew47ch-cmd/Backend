using Auth.Domain.Entities;

namespace Auth.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<PasswordResetToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default);
}
