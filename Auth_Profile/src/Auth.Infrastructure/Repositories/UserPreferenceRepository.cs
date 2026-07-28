using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly MongoDbContext _context;

    public UserPreferenceRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<UserPreference>("user_preferences")
            .Find(up => up.UserId == userId && !up.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<UserPreference>("user_preferences").InsertOneAsync(preference, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<UserPreference>("user_preferences")
            .ReplaceOneAsync(up => up.Id == preference.Id, preference, cancellationToken: cancellationToken);
    }
}
