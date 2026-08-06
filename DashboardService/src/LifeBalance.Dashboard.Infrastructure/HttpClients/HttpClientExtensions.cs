using System.Net.Http.Json;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public static class HttpClientExtensions
{
    public static async Task<T?> GetWrappedAsync<T>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
        where T : class
    {
        var envelope = await client.GetFromJsonAsync<ApiEnvelope<T>>(requestUri, cancellationToken);
        return envelope?.Data;
    }
}
