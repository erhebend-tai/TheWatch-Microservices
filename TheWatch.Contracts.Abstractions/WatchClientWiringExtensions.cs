using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace TheWatch.Contracts.Abstractions;

/// <summary>
/// Standard wiring extensions for inter-service typed HTTP clients.
/// Applies Polly resilience policies (Item 217), X-Correlation-Id propagation (Item 218),
/// and service-to-service API key auth (Item 219) to all typed clients.
/// </summary>
public static class WatchClientWiringExtensions
{
    /// <summary>
    /// Configures a typed HTTP client builder with standard resilience, correlation ID propagation,
    /// and service-to-service API key authentication. Call this after AddXxxClient().
    /// </summary>
    /// <param name="clientBuilder">The IHttpClientBuilder returned by the ServiceRegistration.AddXxxClient() method.</param>
    /// <param name="baseAddress">The base address of the target service (e.g., Aspire service discovery URI).</param>
    /// <returns>The configured IHttpClientBuilder for further chaining.</returns>
    public static IHttpClientBuilder AddWatchClientDefaults(this IHttpClientBuilder clientBuilder, string? baseAddress = null)
    {
        if (!string.IsNullOrEmpty(baseAddress))
        {
            clientBuilder.ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));
        }

        clientBuilder
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddHttpMessageHandler<ServiceApiKeyDelegatingHandler>()
            .AddResilienceHandler($"{clientBuilder.Name}-resilience", ConfigureResilience);

        return clientBuilder;
    }

    /// <summary>
    /// Item 217: Polly resilience policies — retry, circuit breaker, timeout.
    /// Uses Microsoft.Extensions.Http.Resilience for standardized pipeline configuration.
    /// </summary>
    private static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        // Retry: 3 attempts with exponential backoff (1s, 2s, 4s) + jitter
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            ShouldHandle = static args => ValueTask.FromResult(ShouldRetry(args.Outcome))
        });

        // Circuit breaker: open after 5 failures in 30s, stay open for 15s
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = static args => ValueTask.FromResult(ShouldRetry(args.Outcome))
        });

        // Timeout: 30s per-request timeout (inner, inside retry loop)
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Determines whether a response should trigger retry/circuit breaker.
    /// Retries on 5xx server errors, 408 Request Timeout, and 429 Too Many Requests.
    /// Does NOT retry on 4xx client errors (400, 401, 403, 404, 409) — those indicate caller bugs, not transient failures.
    /// </summary>
    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is not null)
            return true; // Network errors, timeouts, etc.

        if (outcome.Result is null)
            return true;

        var statusCode = (int)outcome.Result.StatusCode;
        return statusCode >= 500 || statusCode == 408 || statusCode == 429;
    }

    /// <summary>
    /// Registers the shared delegating handlers (CorrelationIdDelegatingHandler, ServiceApiKeyDelegatingHandler)
    /// in the DI container. Call once during service startup.
    /// </summary>
    public static IServiceCollection AddWatchClientHandlers(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<ServiceApiKeyDelegatingHandler>();
        return services;
    }
}
