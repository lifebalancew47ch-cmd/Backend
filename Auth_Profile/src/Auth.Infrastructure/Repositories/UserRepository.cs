using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<User>("users")
            .Find(u => u.Id == id && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<User>("users")
            .Find(u => u.Email == email.ToLowerInvariant().Trim() && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<User>("users")
            .Find(u => u.Username == username.Trim() && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<User>("users")
            .Find(u => !u.IsDeleted)
            .SortByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<User>("users")
            .CountDocumentsAsync(u => !u.IsDeleted, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant().Trim()),
            Builders<User>.Filter.Eq(u => u.IsDeleted, false));

        if (!string.IsNullOrEmpty(excludeId))
            filter = Builders<User>.Filter.And(filter, Builders<User>.Filter.Ne(u => u.Id, excludeId));

        return await _context.GetCollection<User>("users").Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.Username, username.Trim()),
            Builders<User>.Filter.Eq(u => u.IsDeleted, false));

        if (!string.IsNullOrEmpty(excludeId))
            filter = Builders<User>.Filter.And(filter, Builders<User>.Filter.Ne(u => u.Id, excludeId));

        return await _context.GetCollection<User>("users").Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<User>("users").InsertOneAsync(user, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<User>("users")
            .ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user is not null)
        {
            user.IsDeleted = true;
            user.MarkUpdated();
            await UpdateAsync(user, cancellationToken);
        }
    }
}
