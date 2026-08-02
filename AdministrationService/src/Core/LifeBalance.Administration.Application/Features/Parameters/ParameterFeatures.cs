using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Mappings;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Parameters;

public record ParameterDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string DataType,
    string Value,
    string Category,
    string Status,
    string? MinValue,
    string? MaxValue,
    string Unit,
    int Order,
    bool IsSystem,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// ── Commands ──────────────────────────────────────────────────────────────
public record CreateParameterCommand(
    string Code,
    string Name,
    string Description,
    ParameterDataType DataType,
    string Value,
    string Category,
    string? MinValue = null,
    string? MaxValue = null,
    string Unit = "",
    int Order = 0) : IRequest<ApiResponse<ParameterDto>>;

public record UpdateParameterCommand(
    string Id,
    string Name,
    string Description,
    ParameterDataType DataType,
    string Value,
    string Category,
    string? MinValue = null,
    string? MaxValue = null,
    string Unit = "",
    int Order = 0) : IRequest<ApiResponse<ParameterDto>>;

public record DeleteParameterCommand(string Id) : IRequest<ApiResponse<bool>>;

public record SetParameterStatusCommand(string Id, bool IsActive) : IRequest<ApiResponse<bool>>;

// ── Queries ───────────────────────────────────────────────────────────────
public record GetParameterByIdQuery(string Id) : IRequest<ApiResponse<ParameterDto>>;

public record GetParametersPagedQuery(
    int PageIndex = 1,
    int PageSize = 10,
    string? Search = null,
    string? Category = null,
    bool? OnlyActive = null) : IRequest<ApiResponse<PagedResult<ParameterDto>>>;

// ── Validators ────────────────────────────────────────────────────────────
public class CreateParameterCommandValidator : AbstractValidator<CreateParameterCommand>
{
    public CreateParameterCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(80).Matches("^[A-Za-z0-9_.-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Value).MaximumLength(4000);
        RuleFor(x => x.Unit).MaximumLength(30);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(ValidateNumberRange).WithMessage("MinValue/MaxValue are only allowed for numeric parameters and must be parseable numbers.");
    }

    private static bool ValidateNumberRange(CreateParameterCommand x)
        => x.DataType != ParameterDataType.Number
           || (string.IsNullOrEmpty(x.MinValue) || double.TryParse(x.MinValue, out _))
           && (string.IsNullOrEmpty(x.MaxValue) || double.TryParse(x.MaxValue, out _));
}

public class UpdateParameterCommandValidator : AbstractValidator<UpdateParameterCommand>
{
    public UpdateParameterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Value).MaximumLength(4000);
        RuleFor(x => x.Unit).MaximumLength(30);
    }
}

public class DeleteParameterCommandValidator : AbstractValidator<DeleteParameterCommand>
{
    public DeleteParameterCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

// ── Command Handler ───────────────────────────────────────────────────────
public class ParameterCommandHandler :
    IRequestHandler<CreateParameterCommand, ApiResponse<ParameterDto>>,
    IRequestHandler<UpdateParameterCommand, ApiResponse<ParameterDto>>,
    IRequestHandler<DeleteParameterCommand, ApiResponse<bool>>,
    IRequestHandler<SetParameterStatusCommand, ApiResponse<bool>>
{
    private readonly IRepository<SystemParameter> _parameterRepository;

    public ParameterCommandHandler(IRepository<SystemParameter> parameterRepository)
    {
        _parameterRepository = parameterRepository;
    }

    public async Task<ApiResponse<ParameterDto>> Handle(CreateParameterCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _parameterRepository.FindAsync(x => x.Code == code, cancellationToken);
        if (existing.Any())
        {
            throw new ConflictException($"A parameter with code '{request.Code}' already exists.");
        }

        var parameter = new SystemParameter(
            code,
            request.Name.Trim(),
            request.Description,
            request.DataType,
            request.Value,
            request.Category,
            request.MinValue,
            request.MaxValue,
            request.Unit,
            request.Order);

        await _parameterRepository.AddAsync(parameter, cancellationToken);
        return ApiResponse<ParameterDto>.Ok(AdministrationMappings.ToDto(parameter), "Parameter created successfully.");
    }

    public async Task<ApiResponse<ParameterDto>> Handle(UpdateParameterCommand request, CancellationToken cancellationToken)
    {
        var parameter = await _parameterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(SystemParameter), request.Id);

        if (parameter.IsSystem)
        {
            throw new UnauthorizedOperationException("System parameters cannot be modified.");
        }

        parameter.Update(request.Name.Trim(), request.Description, request.DataType, request.Value,
            request.Category, request.MinValue, request.MaxValue, request.Unit, request.Order);

        await _parameterRepository.UpdateAsync(parameter, cancellationToken);
        return ApiResponse<ParameterDto>.Ok(AdministrationMappings.ToDto(parameter), "Parameter updated successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteParameterCommand request, CancellationToken cancellationToken)
    {
        var parameter = await _parameterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(SystemParameter), request.Id);

        if (parameter.IsSystem)
        {
            throw new UnauthorizedOperationException("System parameters cannot be deleted.");
        }

        await _parameterRepository.SoftDeleteAsync(request.Id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Parameter deleted successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(SetParameterStatusCommand request, CancellationToken cancellationToken)
    {
        var parameter = await _parameterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(SystemParameter), request.Id);

        if (request.IsActive) parameter.Activate();
        else parameter.Deactivate();

        await _parameterRepository.UpdateAsync(parameter, cancellationToken);
        return ApiResponse<bool>.Ok(true, request.IsActive ? "Parameter activated." : "Parameter deactivated.");
    }
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class ParameterQueryHandler :
    IRequestHandler<GetParameterByIdQuery, ApiResponse<ParameterDto>>,
    IRequestHandler<GetParametersPagedQuery, ApiResponse<PagedResult<ParameterDto>>>
{
    private readonly IRepository<SystemParameter> _parameterRepository;

    public ParameterQueryHandler(IRepository<SystemParameter> parameterRepository)
    {
        _parameterRepository = parameterRepository;
    }

    public async Task<ApiResponse<ParameterDto>> Handle(GetParameterByIdQuery request, CancellationToken cancellationToken)
    {
        var parameter = await _parameterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(SystemParameter), request.Id);

        return ApiResponse<ParameterDto>.Ok(AdministrationMappings.ToDto(parameter));
    }

    public async Task<ApiResponse<PagedResult<ParameterDto>>> Handle(GetParametersPagedQuery request, CancellationToken cancellationToken)
    {
        var search = request.Search;
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 100) search = search[..100];
            search = Regex.Escape(search);
        }

        var category = string.IsNullOrWhiteSpace(request.Category) ? null : Regex.Escape(request.Category.Trim());

        var (items, total) = await _parameterRepository.GetPagedAsync(
            x => (string.IsNullOrEmpty(search)
                    || x.Name.Contains(search)
                    || x.Code.Contains(search))
                 && (category == null || x.Category == category)
                 && (request.OnlyActive == null || x.IsActive == request.OnlyActive.Value),
            request.PageIndex,
            request.PageSize,
            x => x.CreatedAt,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<ParameterDto>>.Ok(new PagedResult<ParameterDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
