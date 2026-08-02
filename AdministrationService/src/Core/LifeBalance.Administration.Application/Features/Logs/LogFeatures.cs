using FluentValidation;
using MediatR;
using LifeBalance.Administration.Application.Common.Mappings;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Logs;

public record SystemLogDto(
    string Id,
    string Service,
    string Level,
    string Message,
    string? Exception,
    string? StackTrace,
    string Source,
    string? UserId,
    string CorrelationId,
    DateTime Timestamp);

// ── Ingestion commands ────────────────────────────────────────────────────
public record LogEntryRequest(
    MicroserviceName Service,
    SystemLogLevel Level,
    string Message,
    string? Exception = null,
    string? StackTrace = null,
    string Source = "",
    string? UserId = null,
    string? CorrelationId = null,
    DateTime? Timestamp = null);

public record IngestLogCommand(LogEntryRequest Entry) : IRequest<ApiResponse<SystemLogDto>>;

public record IngestLogsCommand(IReadOnlyList<LogEntryRequest> Entries) : IRequest<ApiResponse<int>>;

// ── Queries ───────────────────────────────────────────────────────────────
public record GetSystemLogByIdQuery(string Id) : IRequest<ApiResponse<SystemLogDto>>;

public record GetSystemLogsPagedQuery(
    int PageIndex = 1,
    int PageSize = 10,
    MicroserviceName? Service = null,
    SystemLogLevel? Level = null,
    string? UserId = null,
    string? CorrelationId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<ApiResponse<PagedResult<SystemLogDto>>>;

public record GetErrorLogsQuery(int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<SystemLogDto>>>;

public record GetWarningLogsQuery(int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<SystemLogDto>>>;

// ── Validators ────────────────────────────────────────────────────────────
public class LogEntryRequestValidator : AbstractValidator<LogEntryRequest>
{
    public LogEntryRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Exception).MaximumLength(8000);
        RuleFor(x => x.StackTrace).MaximumLength(16000);
        RuleFor(x => x.Source).MaximumLength(200);
        RuleFor(x => x.CorrelationId).MaximumLength(128);
    }
}

public class IngestLogsCommandValidator : AbstractValidator<IngestLogsCommand>
{
    public IngestLogsCommandValidator()
    {
        RuleFor(x => x.Entries).NotEmpty().Must(e => e.Count <= 500)
            .WithMessage("A bulk ingest cannot exceed 500 log entries.");
        RuleForEach(x => x.Entries).SetValidator(new LogEntryRequestValidator());
    }
}

// ── Command Handler ───────────────────────────────────────────────────────
public class LogCommandHandler :
    IRequestHandler<IngestLogCommand, ApiResponse<SystemLogDto>>,
    IRequestHandler<IngestLogsCommand, ApiResponse<int>>
{
    private readonly IRepository<SystemLog> _logRepository;

    public LogCommandHandler(IRepository<SystemLog> logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task<ApiResponse<SystemLogDto>> Handle(IngestLogCommand request, CancellationToken cancellationToken)
    {
        var log = ToEntity(request.Entry);
        await _logRepository.AddAsync(log, cancellationToken);
        return ApiResponse<SystemLogDto>.Ok(AdministrationMappings.ToDto(log), "Log registered.");
    }

    public async Task<ApiResponse<int>> Handle(IngestLogsCommand request, CancellationToken cancellationToken)
    {
        foreach (var entry in request.Entries)
        {
            await _logRepository.AddAsync(ToEntity(entry), cancellationToken);
        }
        return ApiResponse<int>.Ok(request.Entries.Count, $"{request.Entries.Count} log entries registered.");
    }

    private static SystemLog ToEntity(LogEntryRequest entry)
        => new(entry.Service, entry.Level, entry.Message, entry.Exception, entry.StackTrace,
               entry.Source, entry.UserId, entry.CorrelationId, entry.Timestamp);
}

// ── Query Handler ─────────────────────────────────────────────────────────
public class LogQueryHandler :
    IRequestHandler<GetSystemLogByIdQuery, ApiResponse<SystemLogDto>>,
    IRequestHandler<GetSystemLogsPagedQuery, ApiResponse<PagedResult<SystemLogDto>>>,
    IRequestHandler<GetErrorLogsQuery, ApiResponse<PagedResult<SystemLogDto>>>,
    IRequestHandler<GetWarningLogsQuery, ApiResponse<PagedResult<SystemLogDto>>>
{
    private readonly IRepository<SystemLog> _logRepository;

    public LogQueryHandler(IRepository<SystemLog> logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task<ApiResponse<SystemLogDto>> Handle(GetSystemLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _logRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(SystemLog), request.Id);

        return ApiResponse<SystemLogDto>.Ok(AdministrationMappings.ToDto(log));
    }

    public async Task<ApiResponse<PagedResult<SystemLogDto>>> Handle(GetSystemLogsPagedQuery request, CancellationToken cancellationToken)
    {
        var userId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId.Trim();

        var (items, total) = await _logRepository.GetPagedAsync(
            x => (request.Service == null || x.Service == request.Service.Value)
                 && (request.Level == null || x.Level == request.Level.Value)
                 && (userId == null || x.UserId == userId)
                 && (correlationId == null || x.CorrelationId == correlationId)
                 && (request.FromDate == null || x.Timestamp >= request.FromDate.Value)
                 && (request.ToDate == null || x.Timestamp <= request.ToDate.Value),
            request.PageIndex,
            request.PageSize,
            x => x.Timestamp,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<SystemLogDto>>.Ok(new PagedResult<SystemLogDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<PagedResult<SystemLogDto>>> Handle(GetErrorLogsQuery request, CancellationToken cancellationToken)
        => await GetByLevelAsync(SystemLogLevel.Error, request.PageIndex, request.PageSize, cancellationToken);

    public async Task<ApiResponse<PagedResult<SystemLogDto>>> Handle(GetWarningLogsQuery request, CancellationToken cancellationToken)
        => await GetByLevelAsync(SystemLogLevel.Warning, request.PageIndex, request.PageSize, cancellationToken);

    private async Task<ApiResponse<PagedResult<SystemLogDto>>> GetByLevelAsync(
        SystemLogLevel level, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        var (items, total) = await _logRepository.GetPagedAsync(
            x => x.Level == level || x.Level == SystemLogLevel.Critical,
            pageIndex,
            pageSize,
            x => x.Timestamp,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<SystemLogDto>>.Ok(new PagedResult<SystemLogDto>(dtos, pageIndex, pageSize, total));
    }
}
