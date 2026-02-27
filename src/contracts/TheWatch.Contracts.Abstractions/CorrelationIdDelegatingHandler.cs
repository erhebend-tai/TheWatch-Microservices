using System.Diagnostics;

namespace TheWatch.Contracts.Abstractions;

/// <summary>
/// Propagates X-Correlation-Id on all outgoing inter-service HTTP requests.
/// If no correlation ID is available from the current Activity or ambient context,
/// a new GUID is generated to ensure every call chain is traceable.
/// Item 218: X-Correlation-Id propagation to all inter-service calls.
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
        {
            // Prefer the current distributed trace ID (OpenTelemetry / Activity)
            var correlationId = Activity.Current?.TraceId.ToString()
                                ?? Guid.NewGuid().ToString("N");

            request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
