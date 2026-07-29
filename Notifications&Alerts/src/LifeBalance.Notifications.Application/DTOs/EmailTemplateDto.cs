using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class EmailTemplateDto
{
    [Required]
    public List<string> To { get; set; } = new();

    [Required]
    public string TemplateId { get; set; } = string.Empty;

    public Dictionary<string, string>? Variables { get; set; }
}
