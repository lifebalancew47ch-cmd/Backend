using LifeBalance.Administration.Domain.Common;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Feature flag / module switch. Allows administrators to enable or disable
/// whole modules of the platform without redeploying.
/// </summary>
public class FeatureFlag : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;
    public bool IsSystem { get; private set; }
    public string? EnabledBy { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public string? DisabledBy { get; private set; }
    public DateTime? DisabledAt { get; private set; }

    private FeatureFlag() { }

    public FeatureFlag(string code, string name, string description, string category, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Flag code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Flag name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description;
        Category = category;
        IsSystem = isSystem;
        IsEnabled = true;
    }

    public void Update(string name, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Flag name is required.", nameof(name));

        Name = name.Trim();
        Description = description;
        Category = category;
        Touch();
    }

    public void Enable(string enabledBy)
    {
        IsEnabled = true;
        EnabledBy = enabledBy;
        EnabledAt = DateTime.UtcNow;
        DisabledBy = null;
        DisabledAt = null;
        Touch();
    }

    public void Disable(string disabledBy)
    {
        IsEnabled = false;
        DisabledBy = disabledBy;
        DisabledAt = DateTime.UtcNow;
        Touch();
    }
}
