namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

// Placeholder for typed HttpClient classes.
//
// Create one typed client per external microservice:
//
// public interface IHabitsServiceClient
// {
//     Task<HabitsDataResponse?> GetUserHabitsAsync(string userId, CancellationToken ct = default);
// }
//
// public sealed class HabitsServiceClient : IHabitsServiceClient
// {
//     private readonly HttpClient _httpClient;
//     public HabitsServiceClient(HttpClient httpClient) => _httpClient = httpClient;
//     ...
// }
//
// Register in Infrastructure/DependencyInjection.cs:
// services.AddHttpClient<IHabitsServiceClient, HabitsServiceClient>(...)
//         .AddStandardResilienceHandler();
