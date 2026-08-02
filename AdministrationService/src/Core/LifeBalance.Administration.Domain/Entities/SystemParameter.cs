using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// A business/technical parameter of the platform (e.g. max score, daily goal,
/// enterprise limits, SaaS plan policy). Stored in a single global collection.
/// </summary>
public class SystemParameter : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ParameterDataType DataType { get; private set; } = ParameterDataType.String;
    public string Value { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; }
    public string? MinValue { get; private set; }
    public string? MaxValue { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private SystemParameter() { }

    public SystemParameter(string code,
                           string name,
                           string description,
                           ParameterDataType dataType,
                           string value,
                           string category,
                           string? minValue = null,
                           string? maxValue = null,
                           string unit = "",
                           int order = 0,
                           bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Parameter code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name is required.", nameof(name));
        if (dataType == ParameterDataType.Json && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A JSON parameter must have a value.", nameof(value));

        Code = code.Trim();
        Name = name.Trim();
        Description = description;
        DataType = dataType;
        Value = value;
        Category = category;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        Order = order;
        IsSystem = isSystem;
        IsActive = true;
    }

    public void Update(string name,
                       string description,
                       ParameterDataType dataType,
                       string value,
                       string category,
                       string? minValue,
                       string? maxValue,
                       string unit,
                       int order)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name is required.", nameof(name));

        Name = name.Trim();
        Description = description;
        DataType = dataType;
        Value = value;
        Category = category;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        Order = order;
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
