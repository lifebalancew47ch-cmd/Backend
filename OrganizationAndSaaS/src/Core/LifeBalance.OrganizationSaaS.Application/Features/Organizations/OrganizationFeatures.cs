using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Enums;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Application.Features.Organizations;

public record OrganizationDto(
    string Id,
    string TenantId,
    string Name,
    string TaxId,
    string Status,
    string PlanId,
    string SubscriptionId,
    string ConfigurationId,
    ContactInfo ContactInfo,
    Address Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrganizationStatsDto(
    string OrganizationId,
    string Name,
    int TotalDepartments,
    int TotalTeams,
    int TotalLicenses,
    int ActiveLicenses,
    int TotalMembers
);

// --- Commands ---
public record CreateOrganizationCommand(
    string Name,
    string TaxId,
    string PlanId,
    ContactInfo ContactInfo,
    Address Address
) : IRequest<ApiResponse<OrganizationDto>>;

public record UpdateOrganizationCommand(
    string Id,
    string Name,
    string TaxId,
    ContactInfo ContactInfo,
    Address Address
) : IRequest<ApiResponse<OrganizationDto>>;

public record ActivateOrganizationCommand(string Id) : IRequest<ApiResponse<bool>>;
public record SuspendOrganizationCommand(string Id) : IRequest<ApiResponse<bool>>;
public record RestoreOrganizationCommand(string Id) : IRequest<ApiResponse<bool>>;
public record ChangeOrganizationPlanCommand(string Id, string NewPlanId) : IRequest<ApiResponse<bool>>;

// --- Queries ---
public record GetOrganizationByIdQuery(string Id) : IRequest<ApiResponse<OrganizationDto>>;
public record GetOrganizationsPagedQuery(int PageIndex = 1, int PageSize = 10, string? Search = null)
    : IRequest<ApiResponse<PagedResult<OrganizationDto>>>;
public record GetOrganizationStatsQuery(string Id) : IRequest<ApiResponse<OrganizationStatsDto>>;

// --- Validators ---
public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PlanId).NotEmpty();
    }
}

// --- Handlers ---
public class OrganizationCommandHandler :
    IRequestHandler<CreateOrganizationCommand, ApiResponse<OrganizationDto>>,
    IRequestHandler<UpdateOrganizationCommand, ApiResponse<OrganizationDto>>,
    IRequestHandler<ActivateOrganizationCommand, ApiResponse<bool>>,
    IRequestHandler<SuspendOrganizationCommand, ApiResponse<bool>>,
    IRequestHandler<RestoreOrganizationCommand, ApiResponse<bool>>,
    IRequestHandler<ChangeOrganizationPlanCommand, ApiResponse<bool>>
{
    private readonly IRepository<Organization> _orgRepository;
    private readonly IRepository<SaaSPlan> _planRepository;
    private readonly ITenantContext _tenantContext;

    public OrganizationCommandHandler(
        IRepository<Organization> orgRepository,
        IRepository<SaaSPlan> planRepository,
        ITenantContext tenantContext)
    {
        _orgRepository = orgRepository;
        _planRepository = planRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ApiResponse<OrganizationDto>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
            tenantId = Guid.NewGuid().ToString("N");

        var organization = new Organization(
            request.Name,
            request.TaxId,
            request.PlanId,
            tenantId,
            request.ContactInfo,
            request.Address
        );

        await _orgRepository.AddAsync(organization, cancellationToken);

        var dto = MapToDto(organization);
        return ApiResponse<OrganizationDto>.Ok(dto, "Organization created successfully.");
    }

    public async Task<ApiResponse<OrganizationDto>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        org.UpdateInfo(request.Name, request.TaxId, request.ContactInfo, request.Address);
        await _orgRepository.UpdateAsync(org, cancellationToken);

        return ApiResponse<OrganizationDto>.Ok(MapToDto(org), "Organization updated successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(ActivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        org.Activate();
        await _orgRepository.UpdateAsync(org, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Organization activated.");
    }

    public async Task<ApiResponse<bool>> Handle(SuspendOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        org.Suspend();
        await _orgRepository.UpdateAsync(org, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Organization suspended.");
    }

    public async Task<ApiResponse<bool>> Handle(RestoreOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        org.Restore();
        await _orgRepository.UpdateAsync(org, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Organization restored.");
    }

    public async Task<ApiResponse<bool>> Handle(ChangeOrganizationPlanCommand request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        var plan = await _planRepository.GetByIdAsync(request.NewPlanId, cancellationToken);
        if (plan == null) throw new ResourceNotFoundException(nameof(SaaSPlan), request.NewPlanId);

        org.ChangePlan(request.NewPlanId);
        await _orgRepository.UpdateAsync(org, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Organization plan updated.");
    }

    private static OrganizationDto MapToDto(Organization org) => new(
        org.Id,
        org.TenantId,
        org.Name,
        org.TaxId,
        org.Status.ToString(),
        org.PlanId,
        org.SubscriptionId,
        org.ConfigurationId,
        org.ContactInfo,
        org.Address,
        org.CreatedAt,
        org.UpdatedAt
    );
}

public class OrganizationQueryHandler :
    IRequestHandler<GetOrganizationByIdQuery, ApiResponse<OrganizationDto>>,
    IRequestHandler<GetOrganizationsPagedQuery, ApiResponse<PagedResult<OrganizationDto>>>,
    IRequestHandler<GetOrganizationStatsQuery, ApiResponse<OrganizationStatsDto>>
{
    private readonly IRepository<Organization> _orgRepository;
    private readonly IRepository<Department> _deptRepository;
    private readonly IRepository<Team> _teamRepository;
    private readonly IRepository<License> _licenseRepository;

    public OrganizationQueryHandler(
        IRepository<Organization> orgRepository,
        IRepository<Department> deptRepository,
        IRepository<Team> teamRepository,
        IRepository<License> licenseRepository)
    {
        _orgRepository = orgRepository;
        _deptRepository = deptRepository;
        _teamRepository = teamRepository;
        _licenseRepository = licenseRepository;
    }

    public async Task<ApiResponse<OrganizationDto>> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        var dto = new OrganizationDto(
            org.Id, org.TenantId, org.Name, org.TaxId, org.Status.ToString(),
            org.PlanId, org.SubscriptionId, org.ConfigurationId, org.ContactInfo,
            org.Address, org.CreatedAt, org.UpdatedAt
        );
        return ApiResponse<OrganizationDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<OrganizationDto>>> Handle(GetOrganizationsPagedQuery request, CancellationToken cancellationToken)
    {
        var search = request.Search;
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100) search = search[..100];
            search = Regex.Escape(search);
        }

        var (items, total) = await _orgRepository.GetPagedAsync(
            x => string.IsNullOrEmpty(search) || x.Name.Contains(search),
            request.PageIndex,
            request.PageSize,
            x => x.CreatedAt,
            sortDescending: true,
            cancellationToken
        );

        var dtos = items.Select(x => new OrganizationDto(
            x.Id, x.TenantId, x.Name, x.TaxId, x.Status.ToString(),
            x.PlanId, x.SubscriptionId, x.ConfigurationId, x.ContactInfo,
            x.Address, x.CreatedAt, x.UpdatedAt
        ));

        var pagedResult = new PagedResult<OrganizationDto>(dtos, request.PageIndex, request.PageSize, total);
        return ApiResponse<PagedResult<OrganizationDto>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<OrganizationStatsDto>> Handle(GetOrganizationStatsQuery request, CancellationToken cancellationToken)
    {
        var org = await _orgRepository.GetByIdAsync(request.Id, cancellationToken);
        if (org == null) throw new ResourceNotFoundException(nameof(Organization), request.Id);

        var depts = await _deptRepository.FindAsync(x => x.OrganizationId == request.Id, cancellationToken);
        var teams = await _teamRepository.FindAsync(x => x.OrganizationId == request.Id, cancellationToken);
        var licenses = await _licenseRepository.FindAsync(x => x.OrganizationId == request.Id, cancellationToken);

        var stats = new OrganizationStatsDto(
            org.Id,
            org.Name,
            depts.Count(),
            teams.Count(),
            licenses.Count(),
            licenses.Count(l => l.Status == LicenseStatus.Assigned),
            depts.Sum(d => d.MemberUserIds.Count)
        );

        return ApiResponse<OrganizationStatsDto>.Ok(stats);
    }
}
