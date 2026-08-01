using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly MongoDbContext _context;

    public PasswordResetTokenRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<PasswordResetToken>("password_reset_tokens")
            .Find(t => t.Token == token)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PasswordResetToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<PasswordResetToken>("password_reset_tokens")
            .Find(t => t.UserId == userId)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<PasswordResetToken>("password_reset_tokens")
            .InsertOneAsync(passwordResetToken, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<PasswordResetToken>("password_reset_tokens")
            .ReplaceOneAsync(t => t.Id == passwordResetToken.Id, passwordResetToken, cancellationToken: cancellationToken);
    }

    public async Task InvalidateExistingForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.And(
            Builders<PasswordResetToken>.Filter.Eq(t => t.UserId, userId),
            Builders<PasswordResetToken>.Filter.Eq(t => t.IsUsed, false));

        var update = Builders<PasswordResetToken>.Update.Set(t => t.IsUsed, true);

        await _context.GetCollection<PasswordResetToken>("password_reset_tokens")
            .UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}
