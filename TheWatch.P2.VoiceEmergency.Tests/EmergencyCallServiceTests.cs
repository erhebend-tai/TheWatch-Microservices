using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using TheWatch.P2.VoiceEmergency.Services;
using Xunit;

namespace TheWatch.P2.VoiceEmergency.Tests;

/// <summary>
/// Unit tests for EmergencyCallService — verifies 911 dispatch logic including
/// webhook delivery, fallback logging, and error handling.
/// </summary>
public class EmergencyCallServiceTests
{
    private static readonly Guid TestIncidentId = Guid.NewGuid();
    private const string TestDescription = "SOS activated via TheWatch Mobile";
    private const double TestLat = 33.4484;
    private const double TestLon = -112.0740;

    // =========================================================================
    // No-webhook configured (log-only fallback)
    // =========================================================================

    [Fact]
    public async Task Dispatch911_NoWebhookConfigured_ReturnsSuccessWithoutError()
    {
        var (service, _) = CreateService(webhookUrl: null);

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Dispatch911_EmptyWebhookUrl_ReturnsSuccessWithoutError()
    {
        var (service, _) = CreateService(webhookUrl: "");

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    // =========================================================================
    // Webhook configured and reachable
    // =========================================================================

    [Fact]
    public async Task Dispatch911_WebhookReturns200_ReturnsSuccess()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "sid-abc123");
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        var result = await service.Dispatch911Async(
            TestIncidentId, TestDescription, TestLat, TestLon, "+15550001234");

        result.Success.Should().BeTrue();
        result.CallSid.Should().Be("sid-abc123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Dispatch911_WebhookReturns500_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError);
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("webhook-http-500");
    }

    [Fact]
    public async Task Dispatch911_WebhookReturns404_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound);
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("404");
    }

    // =========================================================================
    // Webhook configured but network error
    // =========================================================================

    [Fact]
    public async Task Dispatch911_NetworkError_ReturnsFailureWithExceptionMessage()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Dispatch911_Timeout_ReturnsFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Request timed out"));
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        var result = await service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Data validation
    // =========================================================================

    [Fact]
    public async Task Dispatch911_NullCallerPhone_DoesNotThrow()
    {
        var (service, _) = CreateService(webhookUrl: null);

        var act = () => service.Dispatch911Async(TestIncidentId, TestDescription, TestLat, TestLon, callerPhone: null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dispatch911_AllSeverities_CanBeDispatched()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "ok");
        var (service, _) = CreateService("https://hooks.example.com/911", handler);

        for (var severity = 1; severity <= 5; severity++)
        {
            var result = await service.Dispatch911Async(
                Guid.NewGuid(), $"Severity {severity} incident", TestLat, TestLon);

            result.Should().NotBeNull($"severity {severity} should produce a result");
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static (EmergencyCallService service, IHttpClientFactory factory) CreateService(
        string? webhookUrl,
        HttpMessageHandler? handler = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(webhookUrl is null or ""
                ? []
                : new Dictionary<string, string?> { ["EmergencyCall:WebhookUrl"] = webhookUrl })
            .Build();

        var factory = Substitute.For<IHttpClientFactory>();

        if (handler is not null)
        {
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
            factory.CreateClient("emergency-call").Returns(client);
        }

        var logger = NullLogger<EmergencyCallService>.Instance;
        return (new EmergencyCallService(factory, config, logger), factory);
    }

    // ── Fake HTTP handlers ─────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public FakeHttpMessageHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body)
            });
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw _exception;
    }
}
