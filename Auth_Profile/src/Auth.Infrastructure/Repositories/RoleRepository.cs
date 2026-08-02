using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly MongoDbContext _context;

    public RoleRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
            return null;

        return await _context.GetCollection<Role>("roles")
            .Find(r => r.Id == id && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Role>("roles")
            .Find(r => r.NormalizedName == name.ToUpperInvariant() && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GetCollection<Role>("roles")
            .Find(r => !r.IsDeleted)
            .SortBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        // Descarta ids vacíos o que no sean ObjectIds válidos: el driver convierte
        // cada elemento del filtro $in a ObjectId y lanza FormatException (500) si no lo es.
        var idList = (ids ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _))
            .ToList();
        if (idList.Count == 0)
            return [];

        var filter = Builders<Role>.Filter.And(
            Builders<Role>.Filter.In(r => r.Id, idList),
            Builders<Role>.Filter.Eq(r => r.IsDeleted, false));

        return await _context.GetCollection<Role>("roles")
            .Find(filter)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Role>.Filter.And(
            Builders<Role>.Filter.Eq(r => r.NormalizedName, name.ToUpperInvariant()),
            Builders<Role>.Filter.Eq(r => r.IsDeleted, false));

        if (!string.IsNullOrEmpty(excludeId))
            filter = Builders<Role>.Filter.And(filter, Builders<Role>.Filter.Ne(r => r.Id, excludeId));

        return await _context.GetCollection<Role>("roles").Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<Role>("roles").InsertOneAsync(role, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.GetCollection<Role>("roles")
            .ReplaceOneAsync(r => r.Id == role.Id, role, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(id, cancellationToken);
        if (role is not null)
        {
            role.IsDeleted = true;
            role.MarkUpdated();
            await UpdateAsync(role, cancellationToken);
        }
    }
}
