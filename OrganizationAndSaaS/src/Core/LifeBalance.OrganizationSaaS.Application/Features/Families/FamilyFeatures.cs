using FluentValidation;
using MediatR;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;

namespace LifeBalance.OrganizationSaaS.Application.Features.Families;

public record FamilyDto(
    string Id,
    string TenantId,
    string Name,
    string AdministratorUserId,
    List<string> MemberUserIds,
    int MaxMembers,
    DateTime CreatedAt
);

public record CreateFamilyCommand(string Name, string AdministratorUserId, int MaxMembers = 6) : IRequest<ApiResponse<FamilyDto>>;
public record UpdateFamilyCommand(string Id, string Name) : IRequest<ApiResponse<FamilyDto>>;
public record DeleteFamilyCommand(string Id) : IRequest<ApiResponse<bool>>;
public record AddFamilyMemberCommand(string FamilyId, string UserId) : IRequest<ApiResponse<bool>>;
public record RemoveFamilyMemberCommand(string FamilyId, string UserId) : IRequest<ApiResponse<bool>>;
public record TransferFamilyAdminCommand(string FamilyId, string NewAdminUserId) : IRequest<ApiResponse<bool>>;

public record GetFamilyByIdQuery(string Id) : IRequest<ApiResponse<FamilyDto>>;
public record GetFamiliesPagedQuery(int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<FamilyDto>>>;

public class CreateFamilyCommandValidator : AbstractValidator<CreateFamilyCommand>
{
    public CreateFamilyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdministratorUserId).NotEmpty();
        RuleFor(x => x.MaxMembers).GreaterThan(0).LessThanOrEqualTo(20);
    }
}

public class FamilyCommandHandler :
    IRequestHandler<CreateFamilyCommand, ApiResponse<FamilyDto>>,
    IRequestHandler<UpdateFamilyCommand, ApiResponse<FamilyDto>>,
    IRequestHandler<DeleteFamilyCommand, ApiResponse<bool>>,
    IRequestHandler<AddFamilyMemberCommand, ApiResponse<bool>>,
    IRequestHandler<RemoveFamilyMemberCommand, ApiResponse<bool>>,
    IRequestHandler<TransferFamilyAdminCommand, ApiResponse<bool>>
{
    private readonly IRepository<Family> _familyRepository;
    private readonly ITenantContext _tenantContext;

    public FamilyCommandHandler(IRepository<Family> familyRepository, ITenantContext tenantContext)
    {
        _familyRepository = familyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ApiResponse<FamilyDto>> Handle(CreateFamilyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId)) tenantId = Guid.NewGuid().ToString("N");

        var family = new Family(request.Name, request.AdministratorUserId, tenantId, request.MaxMembers);
        await _familyRepository.AddAsync(family, cancellationToken);

        var dto = new FamilyDto(family.Id, family.TenantId, family.Name, family.AdministratorUserId, family.MemberUserIds, family.MaxMembers, family.CreatedAt);
        return ApiResponse<FamilyDto>.Ok(dto, "Family created successfully.");
    }

    public async Task<ApiResponse<FamilyDto>> Handle(UpdateFamilyCommand request, CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (family == null) throw new ResourceNotFoundException(nameof(Family), request.Id);

        family.UpdateName(request.Name);
        await _familyRepository.UpdateAsync(family, cancellationToken);

        var dto = new FamilyDto(family.Id, family.TenantId, family.Name, family.AdministratorUserId, family.MemberUserIds, family.MaxMembers, family.CreatedAt);
        return ApiResponse<FamilyDto>.Ok(dto, "Family updated.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteFamilyCommand request, CancellationToken cancellationToken)
    {
        await _familyRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Family dissolved/deleted.");
    }

    public async Task<ApiResponse<bool>> Handle(AddFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(request.FamilyId, cancellationToken);
        if (family == null) throw new ResourceNotFoundException(nameof(Family), request.FamilyId);

        family.AddMember(request.UserId);
        await _familyRepository.UpdateAsync(family, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Member added to family.");
    }

    public async Task<ApiResponse<bool>> Handle(RemoveFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(request.FamilyId, cancellationToken);
        if (family == null) throw new ResourceNotFoundException(nameof(Family), request.FamilyId);

        family.RemoveMember(request.UserId);
        await _familyRepository.UpdateAsync(family, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Member removed from family.");
    }

    public async Task<ApiResponse<bool>> Handle(TransferFamilyAdminCommand request, CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(request.FamilyId, cancellationToken);
        if (family == null) throw new ResourceNotFoundException(nameof(Family), request.FamilyId);

        family.TransferAdmin(request.NewAdminUserId);
        await _familyRepository.UpdateAsync(family, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Family administrator transferred.");
    }
}

public class FamilyQueryHandler :
    IRequestHandler<GetFamilyByIdQuery, ApiResponse<FamilyDto>>,
    IRequestHandler<GetFamiliesPagedQuery, ApiResponse<PagedResult<FamilyDto>>>
{
    private readonly IRepository<Family> _familyRepository;

    public FamilyQueryHandler(IRepository<Family> familyRepository)
    {
        _familyRepository = familyRepository;
    }

    public async Task<ApiResponse<FamilyDto>> Handle(GetFamilyByIdQuery request, CancellationToken cancellationToken)
    {
        var family = await _familyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (family == null) throw new ResourceNotFoundException(nameof(Family), request.Id);

        var dto = new FamilyDto(family.Id, family.TenantId, family.Name, family.AdministratorUserId, family.MemberUserIds, family.MaxMembers, family.CreatedAt);
        return ApiResponse<FamilyDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<FamilyDto>>> Handle(GetFamiliesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _familyRepository.GetPagedAsync(
            x => true,
            request.PageIndex,
            request.PageSize,
            x => x.CreatedAt,
            sortDescending: true,
            cancellationToken
        );

        var dtos = items.Select(f => new FamilyDto(f.Id, f.TenantId, f.Name, f.AdministratorUserId, f.MemberUserIds, f.MaxMembers, f.CreatedAt));
        return ApiResponse<PagedResult<FamilyDto>>.Ok(new PagedResult<FamilyDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
