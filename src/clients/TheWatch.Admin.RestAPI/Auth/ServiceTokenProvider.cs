namespace TheWatch.Admin.RestAPI.Auth;

/// <summary>
/// Provides service-to-service API keys for system-level calls
/// (e.g., health checks, background jobs) that don't originate from a user request.
/// </summary>
public class ServiceTokenProvider(IConfiguration configuration)
{
    private readonly string _apiKey = configuration["ServiceAuth:ApiKey"] ?? throw new InvalidOperationException("FATAL: ServiceAuth:ApiKey not configured.");

    public string GetServiceApiKey() => _apiKey;

    public void ApplyServiceHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("X-Service-ApiKey", _apiKey);
        request.Headers.TryAddWithoutValidation("X-Gateway-Identity", "TheWatch.Admin.RestAPI");
    }
}
