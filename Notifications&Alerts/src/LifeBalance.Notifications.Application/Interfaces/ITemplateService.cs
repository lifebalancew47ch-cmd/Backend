using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface ITemplateService
{
    Task<List<TemplateDto>> GetAllAsync();
    Task<TemplateDto?> GetByIdAsync(string id);
    Task<TemplateDto> CreateAsync(CreateTemplateDto dto);
    Task<TemplateDto?> UpdateAsync(string id, CreateTemplateDto dto);
    Task<bool> DeleteAsync(string id);
}
