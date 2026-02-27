using System.Net;
using System.Text.Json;

namespace TheWatch.Mobile.Tests.Helpers;

/// <summary>
/// Configurable mock HTTP message handler for testing services that use HttpClient.
/// Supports URL-based routing, request tracking, and sequential response queuing.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<HttpResponseMessage> _responseQueue = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];
    private HttpResponseMessage? _defaultResponse;

    public IReadOnlyList<HttpRequestMessage> SentRequests => _sentRequests.AsReadOnly();
    public int RequestCount => _sentRequests.Count;

    /// <summary>
    /// Register a handler for a specific URL pattern (contains match).
    /// </summary>
    public MockHttpMessageHandler When(string urlContains, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _routes[urlContains] = handler;
        return this;
    }

    /// <summary>
    /// Register a JSON response for a specific URL pattern.
    /// </summary>
    public MockHttpMessageHandler RespondWith<T>(string urlContains, T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _routes[urlContains] = _ => CreateJsonResponse(body, statusCode);
        return this;
    }

    /// <summary>
    /// Register a simple status code response for a URL pattern.
    /// </summary>
    public MockHttpMessageHandler RespondWith(string urlContains, HttpStatusCode statusCode)
    {
        _routes[urlContains] = _ => new HttpResponseMessage(statusCode);
        return this;
    }

    /// <summary>
    /// Enqueue a response to be returned in order (regardless of URL).
    /// </summary>
    public MockHttpMessageHandler Enqueue(HttpResponseMessage response)
    {
        _responseQueue.Enqueue(response);
        return this;
    }

    /// <summary>
    /// Enqueue a JSON response to be returned in order.
    /// </summary>
    public MockHttpMessageHandler Enqueue<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseQueue.Enqueue(CreateJsonResponse(body, statusCode));
        return this;
    }

    /// <summary>
    /// Set a default response for unmatched requests.
    /// </summary>
    public MockHttpMessageHandler SetDefault(HttpStatusCode statusCode = HttpStatusCode.NotFound)
    {
        _defaultResponse = new HttpResponseMessage(statusCode);
        return this;
    }

    /// <summary>
    /// Set a default response that throws an exception (simulates network failure).
    /// </summary>
    public MockHttpMessageHandler SetDefaultThrows(Exception exception)
    {
        _defaultResponse = null;
        _routes["__THROW__"] = _ => throw exception;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sentRequests.Add(request);

        // Check queued responses first
        if (_responseQueue.Count > 0)
            return Task.FromResult(_responseQueue.Dequeue());

        // Check URL-based routes
        var url = request.RequestUri?.ToString() ?? "";
        foreach (var route in _routes)
        {
            if (route.Key == "__THROW__") continue;
            if (url.Contains(route.Key, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(route.Value(request));
        }

        // Check for throw-all
        if (_routes.ContainsKey("__THROW__"))
            return Task.FromResult(_routes["__THROW__"](request));

        // Return default or 404
        return Task.FromResult(_defaultResponse ?? new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Create an HttpClient backed by this mock handler.
    /// </summary>
    public HttpClient CreateClient()
    {
        return new HttpClient(this) { BaseAddress = new Uri("http://localhost") };
    }

    /// <summary>
    /// Verify that a request was sent matching the URL pattern.
    /// </summary>
    public bool WasCalled(string urlContains)
    {
        return _sentRequests.Any(r =>
            r.RequestUri?.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Verify that a request was sent with a specific HTTP method and URL pattern.
    /// </summary>
    public bool WasCalled(HttpMethod method, string urlContains)
    {
        return _sentRequests.Any(r =>
            r.Method == method &&
            r.RequestUri?.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Get all requests matching a URL pattern.
    /// </summary>
    public IEnumerable<HttpRequestMessage> GetCalls(string urlContains)
    {
        return _sentRequests.Where(r =>
            r.RequestUri?.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T body, HttpStatusCode statusCode)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
