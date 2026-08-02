using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Item of a catalog. Embedded inside a <see cref="Catalog"/> aggregate.
/// </summary>
public class CatalogItem
{
    public string Id { get; private set; } = System.Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Value { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>
/// General catalog of the platform (activity types, alert types, notification
/// types, states, administrative roles, categories, ...). Catalogs are global
/// and shared by every other microservice through the REST contract.
/// </summary>
public class Catalog : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public List<CatalogItem> Items { get; private set; } = new();

    private Catalog() { }

    public Catalog(string code, string name, string description, string category, IEnumerable<CatalogItem>? items = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Catalog code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Catalog name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description;
        Category = category;
        Items = items?.ToList() ?? new List<CatalogItem>();
        IsActive = true;
    }

    public void Update(string name, string description, string category, IEnumerable<CatalogItem>? items)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Catalog name is required.", nameof(name));

        Name = name.Trim();
        Description = description;
        Category = category;
        Items = items?.ToList() ?? new List<CatalogItem>();
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
