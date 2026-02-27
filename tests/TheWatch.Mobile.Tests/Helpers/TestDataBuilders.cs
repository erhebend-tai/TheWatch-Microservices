using TheWatch.Shared.Contracts.Mobile;
using TheWatch.Shared.Notifications;

namespace TheWatch.Mobile.Tests.Helpers;

/// <summary>
/// Builder methods for creating test data instances of shared DTOs.
/// </summary>
public static class TestData
{
    public static UserInfoDto CreateUser(
        Guid? id = null,
        string email = "test@example.com",
        string displayName = "Test User",
        string? phone = null,
        string[]? roles = null)
    {
        return new UserInfoDto(
            id ?? Guid.NewGuid(),
            email,
            displayName,
            phone,
            roles ?? ["user"],
            DateTime.UtcNow);
    }

    public static LoginResponse CreateLoginResponse(
        string accessToken = "test-access-token",
        string refreshToken = "test-refresh-token",
        DateTime? expiresAt = null,
        UserInfoDto? user = null)
    {
        return new LoginResponse(
            accessToken,
            refreshToken,
            expiresAt ?? DateTime.UtcNow.AddHours(1),
            user ?? CreateUser());
    }

    public static LocationDto CreateLocation(
        double lat = 40.7128,
        double lon = -74.0060,
        double? accuracy = null,
        DateTime? timestamp = null)
    {
        return new LocationDto(lat, lon, accuracy, timestamp);
    }

    public static IncidentDto CreateIncident(
        Guid? id = null,
        EmergencyType type = EmergencyType.MedicalEmergency,
        string description = "Test incident",
        IncidentStatus status = IncidentStatus.Reported,
        int severity = 3)
    {
        return new IncidentDto(
            id ?? Guid.NewGuid(),
            type,
            description,
            CreateLocation(),
            status,
            Guid.NewGuid(),
            "Reporter",
            severity,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null);
    }

    public static IncidentListResponse CreateIncidentList(int count = 3, int pageSize = 10)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => CreateIncident())
            .ToList();
        return new IncidentListResponse(items, count, 1, pageSize);
    }

    public static FamilyGroupDto CreateFamilyGroup(
        Guid? id = null,
        string name = "Test Family",
        int memberCount = 2)
    {
        var members = Enumerable.Range(0, memberCount)
            .Select(i => new FamilyMemberDto(
                Guid.NewGuid(),
                $"Member {i}",
                $"member{i}@test.com",
                null,
                i == 0 ? FamilyRole.Parent : FamilyRole.Child,
                Guid.NewGuid()))
            .ToList();

        return new FamilyGroupDto(id ?? Guid.NewGuid(), name, members);
    }

    public static VitalReadingDto CreateVitalReading(
        VitalType type = VitalType.HeartRate,
        double value = 72.0,
        string unit = "bpm")
    {
        return new VitalReadingDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            value,
            unit,
            DateTime.UtcNow);
    }

    public static CheckInDto CreateCheckIn(
        CheckInStatus status = CheckInStatus.Safe,
        string? message = null)
    {
        return new CheckInDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreateLocation(),
            status,
            message,
            DateTime.UtcNow);
    }

    public static DeviceRegistration CreateDeviceRegistration(
        Guid? id = null,
        Guid? userId = null,
        string platform = "Android",
        string token = "test-fcm-token")
    {
        return new DeviceRegistration
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Platform = platform,
            DeviceToken = token,
            RegisteredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a valid JWT token string with the specified claims.
    /// Uses a simple structure that JwtSecurityTokenHandler can parse.
    /// </summary>
    public static string CreateJwtToken(
        Guid? userId = null,
        string email = "test@example.com",
        string displayName = "Test User",
        string[]? roles = null,
        DateTime? expires = null)
    {
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"alg":"HS256","typ":"JWT"}"""));

        var claims = new Dictionary<string, object>
        {
            ["sub"] = (userId ?? Guid.NewGuid()).ToString(),
            ["email"] = email,
            ["display_name"] = displayName,
            ["exp"] = new DateTimeOffset(expires ?? DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds(),
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (roles is { Length: > 0 })
        {
            claims["role"] = roles;
        }

        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(claims)));

        // Fake signature (not cryptographically valid but parseable)
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-signature"));

        return $"{header}.{payload}.{signature}";
    }
}
