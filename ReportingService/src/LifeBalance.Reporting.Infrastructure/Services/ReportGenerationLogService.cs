using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.Entities;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.Services;

/// <summary>
/// Persists report generation audit entries. Failures are logged and never surfaced to
/// the caller because the log is purely operational.
/// </summary>
public sealed class ReportGenerationLogService : IReportGenerationLogService
{
    private readonly IReportGenerationLogRepository _repository;
    private readonly ILogger<ReportGenerationLogService> _logger;

    /// <summary>Initializes a new instance of <see cref="ReportGenerationLogService"/>.</summary>
    public ReportGenerationLogService(
        IReportGenerationLogRepository repository,
        ILogger<ReportGenerationLogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LogAsync(
        ReportScope scope,
        string? scopeId,
        string userId,
        ReportFormat? format,
        ReportStatus status,
        double durationMs,
        int recordCount,
        string? errorMessage = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new ReportGenerationLog
            {
                UserId = userId,
                Scope = scope,
                ScopeId = scopeId,
                Format = format,
                Status = status,
                CorrelationId = correlationId ?? string.Empty,
                DurationMs = durationMs,
                RecordCount = recordCount,
                ErrorMessage = errorMessage,
                TimestampUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(log, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist report generation log for UserId: {UserId}, Scope: {Scope}", userId, scope);
        }
    }
}
