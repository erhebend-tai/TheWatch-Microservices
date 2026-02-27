using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P12.Notifications.Notifications;

namespace TheWatch.P12.Notifications.Tests;

public class NotificationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NotificationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Info_ReturnsServiceInfo()
    {
        var response = await _client.GetAsync("/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("P12.Notifications");
    }

    [Fact]
    public async Task SendNotification_ReturnsCreated()
    {
        var request = new SendNotificationRequest(
            RecipientId: Guid.NewGuid(),
            Title: "Test Alert",
            Body: "This is a test notification",
            Channel: NotificationChannel.Push,
            Priority: NotificationPriority.Normal,
            Category: NotificationCategory.General
        );

        var response = await _client.PostAsJsonAsync("/api/notifications/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var notification = await response.Content.ReadFromJsonAsync<NotificationRecord>();
        notification.Should().NotBeNull();
        notification!.Title.Should().Be("Test Alert");
        notification.Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task ListNotifications_ReturnsOk()
    {
        var recipientId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/notifications/{recipientId}?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotification_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/notifications/record/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStats_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/notifications/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<NotificationStats>();
        stats.Should().NotBeNull();
        stats!.DeliveryRate.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SetPreference_ReturnsOk()
    {
        var request = new SetNotificationPreferenceRequest(
            UserId: Guid.NewGuid(),
            Category: NotificationCategory.Emergency,
            PushEnabled: true,
            SmsEnabled: true,
            EmailEnabled: false,
            InAppEnabled: true
        );

        var response = await _client.PostAsJsonAsync("/api/notifications/preferences", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pref = await response.Content.ReadFromJsonAsync<NotificationPreference>();
        pref.Should().NotBeNull();
        pref!.PushEnabled.Should().BeTrue();
        pref.SmsEnabled.Should().BeTrue();
        pref.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetPreferences_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/notifications/preferences/{userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
