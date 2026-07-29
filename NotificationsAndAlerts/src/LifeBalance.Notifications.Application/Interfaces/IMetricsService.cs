using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IMetricsService
{
    Task<MetricsDto> GetGeneralAsync();
    Task<DeliveryMetricsDto> GetDeliveryAsync();
    Task<List<ChannelMetricsDto>> GetChannelsAsync();
    Task<ErrorMetricsDto> GetErrorsAsync();
}
