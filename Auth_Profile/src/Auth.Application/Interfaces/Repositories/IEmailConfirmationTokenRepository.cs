using Auth.Domain.Entities;

namespace Auth.Application.Interfaces.Repositories;

public interface IEmailConfirmationTokenRepository
{
    Task<EmailConfirmationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<EmailConfirmationToken?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default);
    Task InvalidateExistingForUserAsync(string userId, CancellationToken cancellationToken = default);
}
