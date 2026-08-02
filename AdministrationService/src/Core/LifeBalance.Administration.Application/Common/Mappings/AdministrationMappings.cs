using LifeBalance.Administration.Application.Features.Audit;
using LifeBalance.Administration.Application.Features.Catalogs;
using LifeBalance.Administration.Application.Features.FeatureFlags;
using LifeBalance.Administration.Application.Features.Logs;
using LifeBalance.Administration.Application.Features.Parameters;
using LifeBalance.Administration.Domain.Entities;

namespace LifeBalance.Administration.Application.Common.Mappings;

/// <summary>
/// Manual entity → DTO mappings. Kept dependency-free (no AutoMapper) to avoid a
/// known HIGH-severity vulnerability (GHSA-rvv3-g6hj-g44x) and the commercial
/// licence required by patched AutoMapper 15.x versions.
/// </summary>
public static class AdministrationMappings
{
    public static CatalogDto ToDto(Catalog c) => new(
        c.Id,
        c.Code,
        c.Name,
        c.Description,
        c.Category,
        c.IsActive ? "Active" : "Inactive",
        c.Items.Select(i => new CatalogItemDto(
            i.Id, i.Code, i.Name, i.Description, i.Value, i.IsActive, i.SortOrder)).ToList(),
        c.CreatedAt,
        c.UpdatedAt);

    public static ParameterDto ToDto(SystemParameter p) => new(
        p.Id,
        p.Code,
        p.Name,
        p.Description,
        p.DataType.ToString(),
        p.Value,
        p.Category,
        p.IsActive ? "Active" : "Inactive",
        p.MinValue,
        p.MaxValue,
        p.Unit,
        p.Order,
        p.IsSystem,
        p.CreatedAt,
        p.UpdatedAt);

    public static AuditLogDto ToDto(AuditLog a) => new(
        a.Id,
        a.UserId,
        a.UserEmail,
        a.Action,
        a.EntityName,
        a.EntityId,
        a.OperationType.ToString(),
        a.EventType.ToString(),
        a.Service,
        a.Endpoint,
        a.IpAddress,
        a.UserAgent,
        a.CorrelationId,
        a.RequestId,
        a.Result,
        a.DetailsJson,
        a.OrganizationId,
        a.CompanyId,
        a.Timestamp);

    public static SystemLogDto ToDto(SystemLog l) => new(
        l.Id,
        l.Service.ToString(),
        l.Level.ToString(),
        l.Message,
        l.Exception,
        l.StackTrace,
        l.Source,
        l.UserId,
        l.CorrelationId,
        l.Timestamp);

    public static FeatureFlagDto ToDto(FeatureFlag f) => new(
        f.Id,
        f.Code,
        f.Name,
        f.Description,
        f.Category,
        f.IsEnabled ? "Enabled" : "Disabled",
        f.IsSystem,
        f.EnabledBy,
        f.EnabledAt,
        f.DisabledBy,
        f.DisabledAt,
        f.CreatedAt,
        f.UpdatedAt);
}
