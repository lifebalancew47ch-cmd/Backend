using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Mappings;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Catalogs;

public record CatalogItemDto(
    string Id,
    string Code,
    string Name,
    string? Description,
    string? Value,
    bool IsActive,
    int SortOrder);

public record CatalogDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string Category,
    string Status,
    IReadOnlyList<CatalogItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// ── Requests / Commands ───────────────────────────────────────────────────
public record CatalogItemRequest(string Code, string Name, string? Description, string? Value, int SortOrder);

public record CreateCatalogCommand(
    string Code,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<CatalogItemRequest>? Items = null) : IRequest<ApiResponse<CatalogDto>>;

public record UpdateCatalogCommand(
    string Id,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<CatalogItemRequest>? Items = null) : IRequest<ApiResponse<CatalogDto>>;

public record DeleteCatalogCommand(string Id) : IRequest<ApiResponse<bool>>;

public record SetCatalogStatusCommand(string Id, bool IsActive) : IRequest<ApiResponse<bool>>;

// ── Queries ───────────────────────────────────────────────────────────────
public record GetCatalogByIdQuery(string Id) : IRequest<ApiResponse<CatalogDto>>;

public record GetCatalogsPagedQuery(
    int PageIndex = 1,
    int PageSize = 10,
    string? Search = null,
    string? Category = null,
    bool? OnlyActive = null) : IRequest<ApiResponse<PagedResult<CatalogDto>>>;

// ── Validators ────────────────────────────────────────────────────────────
public class CreateCatalogCommandValidator : AbstractValidator<CreateCatalogCommand>
{
    public CreateCatalogCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Items).Must(items => items == null
                || items.All(i => !string.IsNullOrWhiteSpace(i.Code))
                || items.GroupBy(i => i.Code).All(g => g.Count() == 1))
            .WithMessage("Catalog items must have unique non-empty codes.");
    }
}

public class UpdateCatalogCommandValidator : AbstractValidator<UpdateCatalogCommand>
{
    public UpdateCatalogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
    }
}

public class DeleteCatalogCommandValidator : AbstractValidator<DeleteCatalogCommand>
{
    public DeleteCatalogCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetCatalogStatusCommandValidator : AbstractValidator<SetCatalogStatusCommand>
{
    public SetCatalogStatusCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

// ── Command Handler ───────────────────────────────────────────────────────
public class CatalogCommandHandler :
    IRequestHandler<CreateCatalogCommand, ApiResponse<CatalogDto>>,
    IRequestHandler<UpdateCatalogCommand, ApiResponse<CatalogDto>>,
    IRequestHandler<DeleteCatalogCommand, ApiResponse<bool>>,
    IRequestHandler<SetCatalogStatusCommand, ApiResponse<bool>>
{
    private readonly IRepository<Catalog> _catalogRepository;

    public CatalogCommandHandler(IRepository<Catalog> catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<ApiResponse<CatalogDto>> Handle(CreateCatalogCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _catalogRepository.FindAsync(x => x.Code == code, cancellationToken);
        if (existing.Any())
        {
            throw new ConflictException($"A catalog with code '{request.Code}' already exists.");
        }

        var catalog = new Catalog(code, request.Name.Trim(), request.Description, request.Category, MapItems(request.Items));
        await _catalogRepository.AddAsync(catalog, cancellationToken);

        return ApiResponse<CatalogDto>.Ok(AdministrationMappings.ToDto(catalog), "Catalog created successfully.");
    }

    public async Task<ApiResponse<CatalogDto>> Handle(UpdateCatalogCommand request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Catalog), request.Id);

        catalog.Update(request.Name.Trim(), request.Description, request.Category, MapItems(request.Items));
        await _catalogRepository.UpdateAsync(catalog, cancellationToken);

        return ApiResponse<CatalogDto>.Ok(AdministrationMappings.ToDto(catalog), "Catalog updated successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteCatalogCommand request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Catalog), request.Id);

        await _catalogRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Catalog deleted successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(SetCatalogStatusCommand request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Catalog), request.Id);

        if (request.IsActive) catalog.Activate();
        else catalog.Deactivate();

        await _catalogRepository.UpdateAsync(catalog, cancellationToken);
        return ApiResponse<bool>.Ok(true, request.IsActive ? "Catalog activated." : "Catalog deactivated.");
    }

    private static IEnumerable<CatalogItem>? MapItems(IReadOnlyList<CatalogItemRequest>? items)
    {
        if (items == null) return null;
        return items.Select(i => new CatalogItem
        {
            Code = i.Code,
            Name = i.Name,
            Description = i.Description,
            Value = i.Value,
            SortOrder = i.SortOrder
        });
    }
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class CatalogQueryHandler :
    IRequestHandler<GetCatalogByIdQuery, ApiResponse<CatalogDto>>,
    IRequestHandler<GetCatalogsPagedQuery, ApiResponse<PagedResult<CatalogDto>>>
{
    private readonly IRepository<Catalog> _catalogRepository;

    public CatalogQueryHandler(IRepository<Catalog> catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<ApiResponse<CatalogDto>> Handle(GetCatalogByIdQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _catalogRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Catalog), request.Id);

        return ApiResponse<CatalogDto>.Ok(AdministrationMappings.ToDto(catalog));
    }

    public async Task<ApiResponse<PagedResult<CatalogDto>>> Handle(GetCatalogsPagedQuery request, CancellationToken cancellationToken)
    {
        var search = request.Search;
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100) search = search[..100];
            search = Regex.Escape(search);
        }

        var category = string.IsNullOrWhiteSpace(request.Category) ? null : Regex.Escape(request.Category.Trim());

        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasCategory = !string.IsNullOrWhiteSpace(category);
        var onlyActive = request.OnlyActive;

        var (items, total) = await _catalogRepository.GetPagedAsync(
            x => (!hasSearch || x.Name.Contains(search!) || x.Code.Contains(search!))
                 && (!hasCategory || x.Category == category)
                 && (!onlyActive.HasValue || x.IsActive == onlyActive.Value),
            request.PageIndex,
            request.PageSize,
            x => x.CreatedAt,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<CatalogDto>>.Ok(new PagedResult<CatalogDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
