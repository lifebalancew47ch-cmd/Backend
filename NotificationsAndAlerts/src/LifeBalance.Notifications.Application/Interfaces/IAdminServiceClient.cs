namespace LifeBalance.Notifications.Application.Interfaces;

public class GlobalTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AdminConfiguration
{
    public Dictionary<string, string> Parameters { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}

public interface IAdminServiceClient
{
    Task<List<GlobalTemplate>> GetGlobalTemplatesAsync();
    Task<AdminConfiguration?> GetConfigurationAsync();
}
