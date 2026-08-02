using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Mappings;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.FeatureFlags;

public record FeatureFlagDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string Category,
    string Status,
    bool IsSystem,
    string? EnabledBy,
    DateTime? EnabledAt,
    string? DisabledBy,
    DateTime? DisabledAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// ── Commands ──────────────────────────────────────────────────────────────
public record CreateFeatureFlagCommand(
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsSystem = false) : IRequest<ApiResponse<FeatureFlagDto>>;

public record UpdateFeatureFlagCommand(
    string Id,
    string Name,
    string Description,
    string Category) : IRequest<ApiResponse<FeatureFlagDto>>;

public record DeleteFeatureFlagCommand(string Id) : IRequest<ApiResponse<bool>>;

public record SetFeatureFlagStatusCommand(string Id, bool IsEnabled) : IRequest<ApiResponse<bool>>;

// ── Queries ───────────────────────────────────────────────────────────────
public record GetFeatureFlagByIdQuery(string Id) : IRequest<ApiResponse<FeatureFlagDto>>;

public record GetFeatureFlagsPagedQuery(
    int PageIndex = 1,
    int PageSize = 10,
    string? Search = null,
    string? Category = null,
    bool? OnlyEnabled = null) : IRequest<ApiResponse<PagedResult<FeatureFlagDto>>>;

// ── Validators ────────────────────────────────────────────────────────────
public class CreateFeatureFlagCommandValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60).Matches("^[A-Za-z0-9_.-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(400);
        RuleFor(x => x.Category).MaximumLength(80);
    }
}

public class UpdateFeatureFlagCommandValidator : AbstractValidator<UpdateFeatureFlagCommand>
{
    public UpdateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(400);
        RuleFor(x => x.Category).MaximumLength(80);
    }
}

public class SetFeatureFlagStatusCommandValidator : AbstractValidator<SetFeatureFlagStatusCommand>
{
    public SetFeatureFlagStatusCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

// ── Command Handler ───────────────────────────────────────────────────────
public class FeatureFlagCommandHandler :
    IRequestHandler<CreateFeatureFlagCommand, ApiResponse<FeatureFlagDto>>,
    IRequestHandler<UpdateFeatureFlagCommand, ApiResponse<FeatureFlagDto>>,
    IRequestHandler<DeleteFeatureFlagCommand, ApiResponse<bool>>,
    IRequestHandler<SetFeatureFlagStatusCommand, ApiResponse<bool>>
{
    private readonly IRepository<FeatureFlag> _flagRepository;
    private readonly ICurrentUser _currentUser;

    public FeatureFlagCommandHandler(
        IRepository<FeatureFlag> flagRepository,
        ICurrentUser currentUser)
    {
        _flagRepository = flagRepository;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<FeatureFlagDto>> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _flagRepository.FindAsync(x => x.Code == code, cancellationToken);
        if (existing.Any())
        {
            throw new ConflictException($"A feature flag with code '{request.Code}' already exists.");
        }

        var flag = new FeatureFlag(code, request.Name.Trim(), request.Description, request.Category, request.IsSystem);
        flag.Enable(_currentUser.UserId ?? "system");

        await _flagRepository.AddAsync(flag, cancellationToken);
        return ApiResponse<FeatureFlagDto>.Ok(AdministrationMappings.ToDto(flag), "Feature flag created.");
    }

    public async Task<ApiResponse<FeatureFlagDto>> Handle(UpdateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(FeatureFlag), request.Id);

        if (flag.IsSystem)
        {
            throw new UnauthorizedOperationException("System feature flags cannot be modified.");
        }

        flag.Update(request.Name.Trim(), request.Description, request.Category);
        await _flagRepository.UpdateAsync(flag, cancellationToken);
        return ApiResponse<FeatureFlagDto>.Ok(AdministrationMappings.ToDto(flag), "Feature flag updated.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(FeatureFlag), request.Id);

        if (flag.IsSystem)
        {
            throw new UnauthorizedOperationException("System feature flags cannot be deleted.");
        }

        await _flagRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Feature flag deleted.");
    }

    public async Task<ApiResponse<bool>> Handle(SetFeatureFlagStatusCommand request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(FeatureFlag), request.Id);

        var actor = _currentUser.UserId ?? "system";
        if (request.IsEnabled) flag.Enable(actor);
        else flag.Disable(actor);

        await _flagRepository.UpdateAsync(flag, cancellationToken);
        return ApiResponse<bool>.Ok(true, request.IsEnabled ? "Feature flag enabled." : "Feature flag disabled.");
    }
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class FeatureFlagQueryHandler :
    IRequestHandler<GetFeatureFlagByIdQuery, ApiResponse<FeatureFlagDto>>,
    IRequestHandler<GetFeatureFlagsPagedQuery, ApiResponse<PagedResult<FeatureFlagDto>>>
{
    private readonly IRepository<FeatureFlag> _flagRepository;

    public FeatureFlagQueryHandler(IRepository<FeatureFlag> flagRepository)
    {
        _flagRepository = flagRepository;
    }

    public async Task<ApiResponse<FeatureFlagDto>> Handle(GetFeatureFlagByIdQuery request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(FeatureFlag), request.Id);

        return ApiResponse<FeatureFlagDto>.Ok(AdministrationMappings.ToDto(flag));
    }

    public async Task<ApiResponse<PagedResult<FeatureFlagDto>>> Handle(GetFeatureFlagsPagedQuery request, CancellationToken cancellationToken)
    {
        var search = request.Search;
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100) search = search[..100];
            search = Regex.Escape(search);
        }

        var category = string.IsNullOrWhiteSpace(request.Category) ? null : Regex.Escape(request.Category.Trim());

        var (items, total) = await _flagRepository.GetPagedAsync(
            x => (string.IsNullOrEmpty(search)
                    || x.Name.Contains(search)
                    || x.Code.Contains(search))
                 && (category == null || x.Category == category)
                 && (request.OnlyEnabled == null || x.IsEnabled == request.OnlyEnabled.Value),
            request.PageIndex,
            request.PageSize,
            x => x.CreatedAt,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<FeatureFlagDto>>.Ok(new PagedResult<FeatureFlagDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
