using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly MongoDbContext _context;

    public PermissionRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Permission>("permissions")
            .Find(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Permission>("permissions")
            .Find(p => p.NormalizedName == name.ToUpperInvariant() && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Permission>("permissions")
            .Find(p => !p.IsDeleted)
            .SortBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Permission>("permissions")
            .Find(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Permission>.Filter.And(
            Builders<Permission>.Filter.Eq(p => p.NormalizedName, name.ToUpperInvariant()),
            Builders<Permission>.Filter.Eq(p => p.IsDeleted, false));

        if (!string.IsNullOrEmpty(excludeId))
            filter = Builders<Permission>.Filter.And(filter, Builders<Permission>.Filter.Ne(p => p.Id, excludeId));

        return await _context.GetCollection<Permission>("permissions").Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<Permission>("permissions").InsertOneAsync(permission, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<Permission>("permissions")
            .ReplaceOneAsync(p => p.Id == permission.Id, permission, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var permission = await GetByIdAsync(id, cancellationToken);
        if (permission is not null)
        {
            permission.IsDeleted = true;
            permission.MarkUpdated();
            await UpdateAsync(permission, cancellationToken);
        }
    }
}
