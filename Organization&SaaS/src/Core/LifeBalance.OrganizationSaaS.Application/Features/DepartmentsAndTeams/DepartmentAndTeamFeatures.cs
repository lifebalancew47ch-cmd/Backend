using FluentValidation;
using MediatR;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;

namespace LifeBalance.OrganizationSaaS.Application.Features.DepartmentsAndTeams;

// --- DTOs ---
public record DepartmentDto(
    string Id,
    string OrganizationId,
    string TenantId,
    string Name,
    string Description,
    string? ManagerUserId,
    string? ParentDepartmentId,
    List<string> MemberUserIds,
    DateTime CreatedAt
);

public record TeamDto(
    string Id,
    string OrganizationId,
    string TenantId,
    string? DepartmentId,
    string Name,
    string? LeaderUserId,
    List<string> MemberUserIds,
    DateTime CreatedAt
);

// --- Department Commands & Queries ---
public record CreateDepartmentCommand(string OrganizationId, string Name, string Description, string? ManagerUserId = null, string? ParentDepartmentId = null) : IRequest<ApiResponse<DepartmentDto>>;
public record UpdateDepartmentCommand(string Id, string Name, string Description, string? ManagerUserId = null, string? ParentDepartmentId = null) : IRequest<ApiResponse<DepartmentDto>>;
public record DeleteDepartmentCommand(string Id) : IRequest<ApiResponse<bool>>;
public record AssignDepartmentMemberCommand(string DepartmentId, string UserId) : IRequest<ApiResponse<bool>>;
public record RemoveDepartmentMemberCommand(string DepartmentId, string UserId) : IRequest<ApiResponse<bool>>;
public record GetDepartmentByIdQuery(string Id) : IRequest<ApiResponse<DepartmentDto>>;
public record GetDepartmentsPagedQuery(string OrganizationId, int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<DepartmentDto>>>;

// --- Team Commands & Queries ---
public record CreateTeamCommand(string OrganizationId, string Name, string? DepartmentId = null, string? LeaderUserId = null) : IRequest<ApiResponse<TeamDto>>;
public record UpdateTeamCommand(string Id, string Name, string? DepartmentId = null, string? LeaderUserId = null) : IRequest<ApiResponse<TeamDto>>;
public record DeleteTeamCommand(string Id) : IRequest<ApiResponse<bool>>;
public record GetTeamByIdQuery(string Id) : IRequest<ApiResponse<TeamDto>>;
public record GetTeamsPagedQuery(string OrganizationId, int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<TeamDto>>>;

// --- Handlers ---
public class DepartmentAndTeamCommandHandler :
    IRequestHandler<CreateDepartmentCommand, ApiResponse<DepartmentDto>>,
    IRequestHandler<UpdateDepartmentCommand, ApiResponse<DepartmentDto>>,
    IRequestHandler<DeleteDepartmentCommand, ApiResponse<bool>>,
    IRequestHandler<AssignDepartmentMemberCommand, ApiResponse<bool>>,
    IRequestHandler<RemoveDepartmentMemberCommand, ApiResponse<bool>>,
    IRequestHandler<CreateTeamCommand, ApiResponse<TeamDto>>,
    IRequestHandler<UpdateTeamCommand, ApiResponse<TeamDto>>,
    IRequestHandler<DeleteTeamCommand, ApiResponse<bool>>
{
    private readonly IRepository<Department> _deptRepository;
    private readonly IRepository<Team> _teamRepository;
    private readonly ITenantContext _tenantContext;

    public DepartmentAndTeamCommandHandler(
        IRepository<Department> deptRepository,
        IRepository<Team> teamRepository,
        ITenantContext tenantContext)
    {
        _deptRepository = deptRepository;
        _teamRepository = teamRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ApiResponse<DepartmentDto>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var dept = new Department(request.OrganizationId, request.Name, request.Description, tenantId, request.ManagerUserId, request.ParentDepartmentId);
        await _deptRepository.AddAsync(dept, cancellationToken);
        return ApiResponse<DepartmentDto>.Ok(Map(dept), "Department created.");
    }

    public async Task<ApiResponse<DepartmentDto>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var dept = await _deptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (dept == null) throw new ResourceNotFoundException(nameof(Department), request.Id);

        dept.UpdateDetails(request.Name, request.Description, request.ManagerUserId, request.ParentDepartmentId);
        await _deptRepository.UpdateAsync(dept, cancellationToken);
        return ApiResponse<DepartmentDto>.Ok(Map(dept), "Department updated.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        await _deptRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Department deleted.");
    }

    public async Task<ApiResponse<bool>> Handle(AssignDepartmentMemberCommand request, CancellationToken cancellationToken)
    {
        var dept = await _deptRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (dept == null) throw new ResourceNotFoundException(nameof(Department), request.DepartmentId);

        dept.AddMember(request.UserId);
        await _deptRepository.UpdateAsync(dept, cancellationToken);
        return ApiResponse<bool>.Ok(true, "User assigned to department.");
    }

    public async Task<ApiResponse<bool>> Handle(RemoveDepartmentMemberCommand request, CancellationToken cancellationToken)
    {
        var dept = await _deptRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (dept == null) throw new ResourceNotFoundException(nameof(Department), request.DepartmentId);

        dept.RemoveMember(request.UserId);
        await _deptRepository.UpdateAsync(dept, cancellationToken);
        return ApiResponse<bool>.Ok(true, "User removed from department.");
    }

    public async Task<ApiResponse<TeamDto>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var team = new Team(request.OrganizationId, request.Name, tenantId, request.DepartmentId, request.LeaderUserId);
        await _teamRepository.AddAsync(team, cancellationToken);
        return ApiResponse<TeamDto>.Ok(Map(team), "Team created.");
    }

    public async Task<ApiResponse<TeamDto>> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.Id, cancellationToken);
        if (team == null) throw new ResourceNotFoundException(nameof(Team), request.Id);

        team.Update(request.Name, request.DepartmentId, request.LeaderUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
        return ApiResponse<TeamDto>.Ok(Map(team), "Team updated.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        await _teamRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Team deleted.");
    }

    private static DepartmentDto Map(Department d) => new(d.Id, d.OrganizationId, d.TenantId, d.Name, d.Description, d.ManagerUserId, d.ParentDepartmentId, d.MemberUserIds, d.CreatedAt);
    private static TeamDto Map(Team t) => new(t.Id, t.OrganizationId, t.TenantId, t.DepartmentId, t.Name, t.LeaderUserId, t.MemberUserIds, t.CreatedAt);
}

public class DepartmentAndTeamQueryHandler :
    IRequestHandler<GetDepartmentByIdQuery, ApiResponse<DepartmentDto>>,
    IRequestHandler<GetDepartmentsPagedQuery, ApiResponse<PagedResult<DepartmentDto>>>,
    IRequestHandler<GetTeamByIdQuery, ApiResponse<TeamDto>>,
    IRequestHandler<GetTeamsPagedQuery, ApiResponse<PagedResult<TeamDto>>>
{
    private readonly IRepository<Department> _deptRepository;
    private readonly IRepository<Team> _teamRepository;

    public DepartmentAndTeamQueryHandler(IRepository<Department> deptRepository, IRepository<Team> teamRepository)
    {
        _deptRepository = deptRepository;
        _teamRepository = teamRepository;
    }

    public async Task<ApiResponse<DepartmentDto>> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var dept = await _deptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (dept == null) throw new ResourceNotFoundException(nameof(Department), request.Id);
        return ApiResponse<DepartmentDto>.Ok(new DepartmentDto(dept.Id, dept.OrganizationId, dept.TenantId, dept.Name, dept.Description, dept.ManagerUserId, dept.ParentDepartmentId, dept.MemberUserIds, dept.CreatedAt));
    }

    public async Task<ApiResponse<PagedResult<DepartmentDto>>> Handle(GetDepartmentsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _deptRepository.GetPagedAsync(x => x.OrganizationId == request.OrganizationId, request.PageIndex, request.PageSize, cancellationToken: cancellationToken);
        var dtos = items.Select(d => new DepartmentDto(d.Id, d.OrganizationId, d.TenantId, d.Name, d.Description, d.ManagerUserId, d.ParentDepartmentId, d.MemberUserIds, d.CreatedAt));
        return ApiResponse<PagedResult<DepartmentDto>>.Ok(new PagedResult<DepartmentDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<TeamDto>> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.Id, cancellationToken);
        if (team == null) throw new ResourceNotFoundException(nameof(Team), request.Id);
        return ApiResponse<TeamDto>.Ok(new TeamDto(team.Id, team.OrganizationId, team.TenantId, team.DepartmentId, team.Name, team.LeaderUserId, team.MemberUserIds, team.CreatedAt));
    }

    public async Task<ApiResponse<PagedResult<TeamDto>>> Handle(GetTeamsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _teamRepository.GetPagedAsync(x => x.OrganizationId == request.OrganizationId, request.PageIndex, request.PageSize, cancellationToken: cancellationToken);
        var dtos = items.Select(t => new TeamDto(t.Id, t.OrganizationId, t.TenantId, t.DepartmentId, t.Name, t.LeaderUserId, t.MemberUserIds, t.CreatedAt));
        return ApiResponse<PagedResult<TeamDto>>.Ok(new PagedResult<TeamDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
