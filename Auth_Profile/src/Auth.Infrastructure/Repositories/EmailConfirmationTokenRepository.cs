using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly MongoDbContext _context;

    public EmailConfirmationTokenRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<EmailConfirmationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<EmailConfirmationToken>("email_confirmation_tokens")
            .Find(t => t.Token == token)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailConfirmationToken?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<EmailConfirmationToken>("email_confirmation_tokens")
            .Find(t => t.UserId == userId)
            .SortByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<EmailConfirmationToken>("email_confirmation_tokens")
            .InsertOneAsync(token, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<EmailConfirmationToken>("email_confirmation_tokens")
            .ReplaceOneAsync(t => t.Id == token.Id, token, cancellationToken: cancellationToken);
    }
}
