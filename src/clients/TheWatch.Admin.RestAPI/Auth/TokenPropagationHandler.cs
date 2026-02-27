using System.Net.Http.Headers;

namespace TheWatch.Admin.RestAPI.Auth;

/// <summary>
/// Forwards the user's Bearer token to every downstream service call.
/// No implicit trust — every hop carries the original token.
/// </summary>
public class TokenPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            // Forward user's Bearer token
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader["Bearer ".Length..]);
            }

            // Propagate correlation ID
            if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is string cid)
            {
                request.Headers.TryAddWithoutValidation("X-Correlation-Id", cid);
            }
        }

        // Service identity header — downstream services can validate origin
        request.Headers.TryAddWithoutValidation("X-Gateway-Identity", "TheWatch.Admin.RestAPI");

        return base.SendAsync(request, cancellationToken);
    }
}
