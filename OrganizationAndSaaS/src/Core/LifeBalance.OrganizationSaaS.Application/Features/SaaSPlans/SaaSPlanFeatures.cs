using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MediatR;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Application.Features.SaaSPlans;

public record SaaSPlanDto(
    string Id,
    string Name,
    string Tier,
    decimal PriceMonthly,
    decimal PriceYearly,
    string Currency,
    bool IsCustomPricing,
    bool IsHighlighted,
    IReadOnlyList<string> Features,
    PlanLimits Limits,
    bool IsActive);

public class PlanLimitsDto
{
    [Range(0, int.MaxValue)] public int MaxUsers { get; init; }
    [Range(0, int.MaxValue)] public int MaxFamilies { get; init; }
    [Range(0, int.MaxValue)] public int MaxCompanies { get; init; }
    [Range(0, int.MaxValue)] public int MaxDepartments { get; init; }
    [Range(0, int.MaxValue)] public int MaxTeams { get; init; }
    [Range(0, int.MaxValue)] public int MaxLicenses { get; init; }
    [Range(0, int.MaxValue)] public int DataRetentionDays { get; init; }
    public bool DashboardsAvailable { get; init; }
    public bool ReportsAvailable { get; init; }
    public bool IaEnabled { get; init; }
    public bool GamificationEnabled { get; init; }
    public bool NotificationsEnabled { get; init; }
    public bool ApiAccess { get; init; }

    public PlanLimits ToDomain() => new()
    {
        MaxUsers = MaxUsers,
        MaxFamilies = MaxFamilies,
        MaxCompanies = MaxCompanies,
        MaxDepartments = MaxDepartments,
        MaxTeams = MaxTeams,
        MaxLicenses = MaxLicenses,
        DataRetentionDays = DataRetentionDays,
        DashboardsAvailable = DashboardsAvailable,
        ReportsAvailable = ReportsAvailable,
        IaEnabled = IaEnabled,
        GamificationEnabled = GamificationEnabled,
        NotificationsEnabled = NotificationsEnabled,
        ApiAccess = ApiAccess
    };
}

public record CreateSaaSPlanCommand(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30)] string Tier,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PriceMonthly,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PriceYearly,
    [property: Required, RegularExpression("^[A-Za-z]{3}$")] string Currency,
    bool IsCustomPricing,
    bool IsHighlighted,
    [property: MaxLength(50)] List<string> Features,
    [property: Required] PlanLimitsDto Limits) : IRequest<ApiResponse<SaaSPlanDto>>;

public record UpdateSaaSPlanCommand(
    string Id,
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, StringLength(30)] string Tier,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PriceMonthly,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PriceYearly,
    [property: Required, RegularExpression("^[A-Za-z]{3}$")] string Currency,
    bool IsCustomPricing,
    bool IsHighlighted,
    [property: MaxLength(50)] List<string> Features,
    [property: Required] PlanLimitsDto Limits) : IRequest<ApiResponse<SaaSPlanDto>>;

public record SetSaaSPlanActiveCommand(string Id, bool IsActive) : IRequest<ApiResponse<bool>>;
public record GetActiveSaaSPlansQuery(int Limit = 100) : IRequest<ApiResponse<IReadOnlyList<SaaSPlanDto>>>;
public record GetSaaSPlanByIdQuery(string Id) : IRequest<ApiResponse<SaaSPlanDto>>;

public class SaaSPlanCommandHandler :
    IRequestHandler<CreateSaaSPlanCommand, ApiResponse<SaaSPlanDto>>,
    IRequestHandler<UpdateSaaSPlanCommand, ApiResponse<SaaSPlanDto>>,
    IRequestHandler<SetSaaSPlanActiveCommand, ApiResponse<bool>>
{
    private readonly IRepository<SaaSPlan> _planRepository;
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly ITenantContext _tenantContext;

    public SaaSPlanCommandHandler(
        IRepository<SaaSPlan> planRepository,
        IRepository<AuditLog> auditRepository,
        ITenantContext tenantContext)
    {
        _planRepository = planRepository;
        _auditRepository = auditRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ApiResponse<SaaSPlanDto>> Handle(CreateSaaSPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = GetAuditUserId();
        var plan = new SaaSPlan(request.Name, ParseTier(request.Tier), request.PriceMonthly, request.PriceYearly,
            request.Limits.ToDomain(), request.Currency, request.IsCustomPricing, request.IsHighlighted, request.Features);

        await _planRepository.AddAsync(plan, cancellationToken);
        await AuditAsync(userId, "Create", plan, cancellationToken);
        return ApiResponse<SaaSPlanDto>.Ok(SaaSPlanMapper.Map(plan), "Plan created successfully.");
    }

    public async Task<ApiResponse<SaaSPlanDto>> Handle(UpdateSaaSPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = GetAuditUserId();
        var plan = await GetRequiredAsync(request.Id, cancellationToken);
        plan.Update(request.Name, ParseTier(request.Tier), request.PriceMonthly, request.PriceYearly,
            request.Limits.ToDomain(), request.Currency, request.IsCustomPricing, request.IsHighlighted, request.Features);

        await _planRepository.UpdateAsync(plan, cancellationToken);
        await AuditAsync(userId, "Update", plan, cancellationToken);
        return ApiResponse<SaaSPlanDto>.Ok(SaaSPlanMapper.Map(plan), "Plan updated successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(SetSaaSPlanActiveCommand request, CancellationToken cancellationToken)
    {
        var userId = GetAuditUserId();
        var plan = await GetRequiredAsync(request.Id, cancellationToken);
        if (request.IsActive) plan.Activate(); else plan.Deactivate();

        await _planRepository.UpdateAsync(plan, cancellationToken);
        await AuditAsync(userId, request.IsActive ? "Activate" : "Deactivate", plan, cancellationToken);
        return ApiResponse<bool>.Ok(true, request.IsActive ? "Plan activated." : "Plan deactivated.");
    }

    private async Task<SaaSPlan> GetRequiredAsync(string id, CancellationToken cancellationToken)
        => await _planRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new ResourceNotFoundException(nameof(SaaSPlan), id);

    private string GetAuditUserId()
    {
        var userId = _tenantContext.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Authenticated user identifier is required.");
        return userId;
    }

    private async Task AuditAsync(string userId, string action, SaaSPlan plan, CancellationToken cancellationToken)
    {
        var changes = JsonSerializer.Serialize(new { plan.Name, Tier = SaaSPlanMapper.MapTier(plan.Tier), plan.IsActive });
        var audit = new AuditLog(userId, action, nameof(SaaSPlan), plan.Id, changes, _tenantContext.CorrelationId);
        await _auditRepository.AddAsync(audit, cancellationToken);
    }

    private static PlanTier ParseTier(string tier)
    {
        var normalized = tier.Trim();
        if (normalized.Equals("Individual", StringComparison.OrdinalIgnoreCase)) return PlanTier.Personal;
        if (normalized.Equals("Corporativo", StringComparison.OrdinalIgnoreCase)) return PlanTier.Business;
        if (Enum.TryParse<PlanTier>(normalized, true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        throw new DomainException("Tier must be a valid plan tier.");
    }
}

public class SaaSPlanQueryHandler :
    IRequestHandler<GetActiveSaaSPlansQuery, ApiResponse<IReadOnlyList<SaaSPlanDto>>>,
    IRequestHandler<GetSaaSPlanByIdQuery, ApiResponse<SaaSPlanDto>>
{
    private readonly IRepository<SaaSPlan> _planRepository;

    public SaaSPlanQueryHandler(IRepository<SaaSPlan> planRepository) => _planRepository = planRepository;

    public async Task<ApiResponse<IReadOnlyList<SaaSPlanDto>>> Handle(GetActiveSaaSPlansQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var (plans, _) = await _planRepository.GetPagedAsync(
            plan => plan.IsActive, 1, limit, plan => plan.Name, cancellationToken: cancellationToken);

        if (!plans.Any())
        {
            var seedPlans = new List<SaaSPlan>
            {
                new("Individual", PlanTier.Personal, 29, 348, new PlanLimits { MaxUsers = 1, MaxFamilies = 1, MaxCompanies = 1, MaxDepartments = 1, MaxTeams = 1, MaxLicenses = 1, DataRetentionDays = 30, DashboardsAvailable = false, ReportsAvailable = false, IaEnabled = false, GamificationEnabled = false, NotificationsEnabled = true, ApiAccess = false }, "USD", false, false, new[] { "1 dispositivo LifeBalance Watch", "Métricas biométricas básicas", "Historial de 30 días", "Soporte por correo" }),
                new("Corporativo", PlanTier.Business, 199, 2388, new PlanLimits { MaxUsers = 25, MaxFamilies = 1, MaxCompanies = 1, MaxDepartments = 10, MaxTeams = 25, MaxLicenses = 25, DataRetentionDays = 365, DashboardsAvailable = true, ReportsAvailable = true, IaEnabled = true, GamificationEnabled = true, NotificationsEnabled = true, ApiAccess = true }, "USD", false, true, new[] { "Hasta 25 licencias de equipo", "Dashboard ejecutivo en tiempo real", "Integraciones API ilimitadas", "Soporte prioritario 24/7" }),
                new("Enterprise", PlanTier.Enterprise, 0, 0, PlanLimits.DefaultEnterprise(), "USD", true, false, new[] { "Licencias ilimitadas", "Infraestructura dedicada y SLA personalizado", "Onboarding y consultoría in-house", "Gerente de cuenta dedicado" })
            };

            foreach (var plan in seedPlans)
            {
                await _planRepository.AddAsync(plan, cancellationToken);
            }

            var pagedResult = await _planRepository.GetPagedAsync(
                plan => plan.IsActive, 1, limit, plan => plan.Name, cancellationToken: cancellationToken);
            plans = pagedResult.Items;
        }

        return ApiResponse<IReadOnlyList<SaaSPlanDto>>.Ok(plans.Select(SaaSPlanMapper.Map).ToList());
    }

    public async Task<ApiResponse<SaaSPlanDto>> Handle(GetSaaSPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new ResourceNotFoundException(nameof(SaaSPlan), request.Id);
        return ApiResponse<SaaSPlanDto>.Ok(SaaSPlanMapper.Map(plan));
    }
}

internal static class SaaSPlanMapper
{
    public static SaaSPlanDto Map(SaaSPlan plan) => new(
        plan.Id, plan.Name, MapTier(plan.Tier), plan.PriceMonthly, plan.PriceYearly, plan.Currency,
        plan.IsCustomPricing, plan.IsHighlighted, plan.Features, plan.Limits, plan.IsActive);

    public static string MapTier(PlanTier tier) => tier switch
    {
        PlanTier.Personal => "Individual",
        PlanTier.Business => "Corporativo",
        _ => tier.ToString()
    };
}
