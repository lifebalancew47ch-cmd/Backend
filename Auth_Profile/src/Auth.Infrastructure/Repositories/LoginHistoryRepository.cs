using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly MongoDbContext _context;

    public LoginHistoryRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoginHistory loginHistory, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<LoginHistory>("login_history").InsertOneAsync(loginHistory, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<LoginHistory>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<LoginHistory>("login_history")
            .Find(l => l.UserId == userId)
            .SortByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoginHistory>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<LoginHistory>("login_history")
            .Find(_ => true)
            .SortByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<LoginHistory>("login_history")
            .CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);
    }
}
