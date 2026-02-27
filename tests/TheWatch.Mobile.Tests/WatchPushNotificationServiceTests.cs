using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for push notification service logic — deep link routing, notification data model,
/// default topics, and registration state.
/// Since WatchPushNotificationService depends on MAUI push notification APIs,
/// we test the pure routing/mapping logic and model defaults independently.
/// </summary>
public class WatchPushNotificationServiceTests
{
    // =========================================================================
    // GetDeepLinkRoute — By Data Key
    // =========================================================================

    [Fact]
    public void GetDeepLinkRoute_IncidentId_ReturnsSos()
    {
        var data = new Dictionary<string, string> { ["incidentId"] = Guid.NewGuid().ToString() };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/sos");
    }

    [Fact]
    public void GetDeepLinkRoute_DispatchId_ReturnsMap()
    {
        var data = new Dictionary<string, string> { ["dispatchId"] = Guid.NewGuid().ToString() };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/map");
    }

    [Fact]
    public void GetDeepLinkRoute_AlertId_ReturnsHealth()
    {
        var data = new Dictionary<string, string> { ["alertId"] = Guid.NewGuid().ToString() };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/health");
    }

    [Fact]
    public void GetDeepLinkRoute_EvidenceId_ReturnsEvidence()
    {
        var data = new Dictionary<string, string> { ["evidenceId"] = Guid.NewGuid().ToString() };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/evidence");
    }

    [Fact]
    public void GetDeepLinkRoute_ReportId_WithValidGuid_ReturnsReportRoute()
    {
        var reportId = Guid.NewGuid();
        var data = new Dictionary<string, string> { ["reportId"] = reportId.ToString() };

        var route = GetDeepLinkRoute(data);

        route.Should().Be($"/report/{reportId}");
    }

    [Fact]
    public void GetDeepLinkRoute_ReportId_WithInvalidGuid_ReturnsRoot()
    {
        var data = new Dictionary<string, string> { ["reportId"] = "not-a-guid" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/");
    }

    // =========================================================================
    // GetDeepLinkRoute — By Source
    // =========================================================================

    [Fact]
    public void GetDeepLinkRoute_SourceVoiceEmergency_ReturnsSos()
    {
        var data = new Dictionary<string, string> { ["source"] = "VoiceEmergency" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/sos");
    }

    [Fact]
    public void GetDeepLinkRoute_SourceFamilyHealth_ReturnsHealth()
    {
        var data = new Dictionary<string, string> { ["source"] = "FamilyHealth" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/health");
    }

    [Fact]
    public void GetDeepLinkRoute_SourceDisasterRelief_ReturnsMap()
    {
        var data = new Dictionary<string, string> { ["source"] = "DisasterRelief" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/map");
    }

    [Fact]
    public void GetDeepLinkRoute_SourceFirstResponder_ReturnsMap()
    {
        var data = new Dictionary<string, string> { ["source"] = "FirstResponder" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/map");
    }

    [Fact]
    public void GetDeepLinkRoute_SourceGamification_ReturnsProfile()
    {
        var data = new Dictionary<string, string> { ["source"] = "Gamification" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/profile");
    }

    [Fact]
    public void GetDeepLinkRoute_SourceEvidence_ReturnsEvidence()
    {
        var data = new Dictionary<string, string> { ["source"] = "Evidence" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/evidence");
    }

    [Fact]
    public void GetDeepLinkRoute_UnknownSource_ReturnsRoot()
    {
        var data = new Dictionary<string, string> { ["source"] = "UnknownService" };

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/");
    }

    [Fact]
    public void GetDeepLinkRoute_EmptyData_ReturnsRoot()
    {
        var data = new Dictionary<string, string>();

        var route = GetDeepLinkRoute(data);

        route.Should().Be("/");
    }

    // =========================================================================
    // PushNotificationData Model
    // =========================================================================

    [Fact]
    public void PushNotificationData_Defaults()
    {
        var notification = new PushNotificationData();

        notification.Title.Should().BeEmpty();
        notification.Body.Should().BeEmpty();
        notification.Data.Should().NotBeNull();
        notification.Data.Should().BeEmpty();
    }

    [Fact]
    public void PushNotificationData_CanSetAllProperties()
    {
        var notification = new PushNotificationData
        {
            Title = "Emergency Alert",
            Body = "Fire reported near your location",
            Data = new Dictionary<string, string>
            {
                ["incidentId"] = Guid.NewGuid().ToString(),
                ["source"] = "VoiceEmergency"
            }
        };

        notification.Title.Should().Be("Emergency Alert");
        notification.Body.Should().Contain("Fire");
        notification.Data.Should().ContainKey("incidentId");
    }

    // =========================================================================
    // Default Topics
    // =========================================================================

    [Fact]
    public void DefaultTopics_ContainsThreeItems()
    {
        var topics = GetDefaultTopics();

        topics.Should().HaveCount(3);
    }

    [Fact]
    public void DefaultTopics_ContainsExpectedValues()
    {
        var topics = GetDefaultTopics();

        topics.Should().Contain("watch-voiceemergency");
        topics.Should().Contain("watch-familyhealth");
        topics.Should().Contain("watch-disasterrelief");
    }

    // =========================================================================
    // Registration State
    // =========================================================================

    [Fact]
    public void IsRegistered_DefaultIsFalse()
    {
        var state = new PushRegistrationState();

        state.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void CurrentToken_DefaultIsNull()
    {
        var state = new PushRegistrationState();

        state.CurrentToken.Should().BeNull();
    }

    [Fact]
    public void Registration_SetsTokenAndState()
    {
        var state = new PushRegistrationState();

        state.CurrentToken = "fcm-token-abc123";
        state.IsRegistered = true;

        state.CurrentToken.Should().Be("fcm-token-abc123");
        state.IsRegistered.Should().BeTrue();
    }

    // =========================================================================
    // Mirrors WatchPushNotificationService logic
    // =========================================================================

    private static string GetDeepLinkRoute(Dictionary<string, string> data)
    {
        if (data.ContainsKey("incidentId")) return "/sos";
        if (data.ContainsKey("dispatchId")) return "/map";
        if (data.ContainsKey("alertId")) return "/health";
        if (data.ContainsKey("evidenceId")) return "/evidence";

        if (data.TryGetValue("reportId", out var reportId))
        {
            return Guid.TryParse(reportId, out var guid) ? $"/report/{guid}" : "/";
        }

        if (data.TryGetValue("source", out var source))
        {
            return source switch
            {
                "VoiceEmergency" => "/sos",
                "FamilyHealth" => "/health",
                "DisasterRelief" => "/map",
                "FirstResponder" => "/map",
                "Gamification" => "/profile",
                "Evidence" => "/evidence",
                _ => "/"
            };
        }

        return "/";
    }

    private static List<string> GetDefaultTopics()
    {
        return ["watch-voiceemergency", "watch-familyhealth", "watch-disasterrelief"];
    }
}

/// <summary>
/// Mirror of PushNotificationData from TheWatch.Mobile.Services
/// </summary>
public class PushNotificationData
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public Dictionary<string, string> Data { get; set; } = new();
}

/// <summary>
/// Mirror of push notification registration state from TheWatch.Mobile.Services
/// </summary>
public class PushRegistrationState
{
    public bool IsRegistered { get; set; }
    public string? CurrentToken { get; set; }
}
