using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using LifeBalance.Administration.Application.Interfaces;

namespace LifeBalance.Administration.Infrastructure.ExternalServices;

/// <summary>
/// Base for typed upstream microservice clients. Health probes measure latency,
/// never throw, and return a structured <see cref="ServiceHealthResult"/> so the
/// monitoring board can keep operating even when an upstream is down.
/// </summary>
public abstract class BaseServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _serviceName;

    protected BaseServiceClient(HttpClient httpClient, ILogger logger, string serviceName)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceName = serviceName;
    }

    /// <summary>Health endpoint of the upstream service.</summary>
    protected virtual string HealthPath => "/health";

    public async Task<ServiceHealthResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.GetAsync(HealthPath, cancellationToken);
            stopwatch.Stop();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            object? payload = string.IsNullOrWhiteSpace(body) || body.Length > 2000 ? null : TryParse(body);

            var healthy = response.IsSuccessStatusCode;
            return new ServiceHealthResult(
                healthy,
                (int)response.StatusCode,
                healthy ? "Healthy" : $"Service returned HTTP {(int)response.StatusCode}",
                stopwatch.ElapsedMilliseconds,
                null,
                payload);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("Health probe for {Service} failed: {Message}", _serviceName, ex.Message);
            return new ServiceHealthResult(false, null, "Service unreachable", stopwatch.ElapsedMilliseconds, null, null);
        }
    }

    /// <summary>
    /// Best-effort JSON fetch used for data enrichment. Returns null when the
    /// upstream is down, the endpoint does not exist or the body is not JSON.
    /// </summary>
    protected async Task<object?> TryGetJsonAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return TryParse(body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Data enrichment for {Service} at {Path} failed: {Message}", _serviceName, path, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Best-effort fetch of a typed list from the standard <c>Response&lt;T&gt;</c>
    /// envelope (<c>data</c> array) used across all LifeBalance services.
    /// Returns null when the upstream is down, unauthorized or malformed so
    /// callers can apply the fail-closed policy (null => 503).
    /// </summary>
    protected async Task<IReadOnlyList<T>?> TryGetListAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("data", out var dataElement)
                || dataElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<IReadOnlyList<T>>(dataElement.GetRawText(), options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Typed fetch for {Service} at {Path} failed: {Message}", _serviceName, path, ex.Message);
            return null;
        }
    }

    private static object? TryParse(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
