using Microsoft.Extensions.Configuration;

namespace TheWatch.Contracts.Abstractions;

/// <summary>
/// Attaches a pre-shared API key to every outgoing inter-service HTTP request
/// via the X-Api-Key header. The receiving service validates this key using
/// ApiKeyAuthHandler in TheWatch.Shared.Security.
/// Item 219: Service-to-service API key auth on all typed clients.
/// </summary>
public class ServiceApiKeyDelegatingHandler(IConfiguration configuration) : DelegatingHandler
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string ServiceIdentityHeader = "X-Service-Identity";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Attach service-to-service API key if configured
        var apiKey = configuration["Security:ServiceApiKey"];
        if (!string.IsNullOrEmpty(apiKey) && !request.Headers.Contains(ApiKeyHeader))
        {
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, apiKey);
        }

        // Attach caller service identity for audit logging
        var serviceName = configuration["ServiceIdentity"] ?? "unknown";
        if (!request.Headers.Contains(ServiceIdentityHeader))
        {
            request.Headers.TryAddWithoutValidation(ServiceIdentityHeader, serviceName);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
