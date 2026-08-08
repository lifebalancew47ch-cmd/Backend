namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record IngestionHistoryItemDto(string? Id, string? EventType, DateTime? OccurredAtUtc, string? Status);
public record IngestionSyncBatchStatusDto(string? ClientBatchId, string? Status, DateTime? ReceivedAtUtc, int? ItemCount);

public interface IIngestionServiceClient
{
    Task<List<IngestionHistoryItemDto>?> GetHistoryAsync(string? type, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IngestionSyncBatchStatusDto?> GetSyncBatchAsync(string clientBatchId, CancellationToken cancellationToken = default);
}