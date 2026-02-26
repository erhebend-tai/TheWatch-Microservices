using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TheWatch.Contracts.Abstractions;

/// <summary>
/// Base HTTP client with standardized error handling, JSON serialization, and correlation ID propagation.
/// All per-service clients inherit from this.
/// </summary>
public abstract class ServiceClientBase
{
    protected readonly HttpClient Http;
    protected readonly string ServiceName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected ServiceClientBase(HttpClient http, string serviceName)
    {
        Http = http;
        ServiceName = serviceName;
    }

    protected async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await Http.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    protected async Task<T> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    protected async Task PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task<T> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await Http.PutAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    protected async Task PutAsync(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await Http.PutAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    protected async Task<T> DeleteAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await Http.DeleteAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    protected async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var response = await Http.DeleteAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var correlationId = response.Headers.TryGetValues("X-Correlation-Id", out var values)
            ? values.FirstOrDefault() : null;

        throw new ServiceClientException(
            ServiceName,
            $"{ServiceName} returned {(int)response.StatusCode} {response.StatusCode}: {TruncateBody(body)}",
            response.StatusCode,
            body,
            correlationId);
    }

    private static string TruncateBody(string body)
        => body.Length > 500 ? body[..500] + "..." : body;
}
