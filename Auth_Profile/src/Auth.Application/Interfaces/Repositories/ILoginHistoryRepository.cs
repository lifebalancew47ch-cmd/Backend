using Auth.Domain.Entities;

namespace Auth.Application.Interfaces.Repositories;

public interface ILoginHistoryRepository
{
    Task AddAsync(LoginHistory loginHistory, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoginHistory>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoginHistory>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
