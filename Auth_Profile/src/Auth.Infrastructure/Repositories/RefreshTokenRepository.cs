using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MongoDbContext _context;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<RefreshToken>("refresh_tokens")
            .Find(rt => rt.Token == token)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<RefreshToken>("refresh_tokens")
            .Find(rt => rt.UserId == userId && rt.IsActive)
            .SortByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<RefreshToken>("refresh_tokens")
            .Find(rt => rt.UserId == userId)
            .SortByDescending(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<RefreshToken>("refresh_tokens").InsertOneAsync(refreshToken, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<RefreshToken>("refresh_tokens")
            .ReplaceOneAsync(rt => rt.Id == refreshToken.Id, refreshToken, cancellationToken: cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(string userId, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.And(
            Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId),
            Builders<RefreshToken>.Filter.Eq(rt => rt.IsActive, true));

        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.IsActive, false)
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.RevokedByIp, revokedByIp ?? "system");

        await _context.GetCollection<RefreshToken>("refresh_tokens")
            .UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}
