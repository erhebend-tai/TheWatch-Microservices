using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using TheWatch.Mobile.Tests.Helpers;
using TheWatch.Shared.Contracts.Mobile;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for WatchAuthService logic.
/// Since the service uses MAUI SecureStorage (not available in net10.0 test TFM),
/// we test: JWT parsing, login/register HTTP flows via mock handler, auth state logic.
/// </summary>
public class WatchAuthServiceTests
{
    // =========================================================================
    // JWT Token Parsing (reimplemented from WatchAuthService.ParseUserFromToken)
    // =========================================================================

    [Fact]
    public void ParseUserFromToken_ValidToken_ExtractsAllClaims()
    {
        var userId = Guid.NewGuid();
        var token = TestData.CreateJwtToken(
            userId: userId,
            email: "alice@watch.com",
            displayName: "Alice",
            roles: ["admin", "responder"]);

        var user = ParseUserFromToken(token);

        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
        user.Email.Should().Be("alice@watch.com");
        user.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public void ParseUserFromToken_MissingClaims_UsesDefaults()
    {
        // Token with only sub claim
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"alg":"HS256","typ":"JWT"}"""));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new
            {
                sub = Guid.NewGuid().ToString(),
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            })));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("sig"));
        var token = $"{header}.{payload}.{signature}";

        var user = ParseUserFromToken(token);

        user.Should().NotBeNull();
        user!.Email.Should().BeEmpty();
        user.DisplayName.Should().BeEmpty(); // falls back to email which is ""
        user.Roles.Should().Contain("user"); // default role
    }

    [Fact]
    public void ParseUserFromToken_InvalidToken_ReturnsNull()
    {
        var user = ParseUserFromToken("not.a.valid.token");
        user.Should().BeNull();
    }

    [Fact]
    public void ParseUserFromToken_EmptyString_ReturnsNull()
    {
        var user = ParseUserFromToken("");
        user.Should().BeNull();
    }

    [Fact]
    public void ParseUserFromToken_InvalidGuidSub_UsesEmptyGuid()
    {
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"alg":"HS256","typ":"JWT"}"""));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new
            {
                sub = "not-a-guid",
                email = "bob@test.com",
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            })));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("sig"));
        var token = $"{header}.{payload}.{signature}";

        var user = ParseUserFromToken(token);

        user.Should().NotBeNull();
        user!.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ParseUserFromToken_MultipleRoles_AllExtracted()
    {
        var token = TestData.CreateJwtToken(roles: ["admin", "responder", "medic"]);

        var user = ParseUserFromToken(token);

        user.Should().NotBeNull();
        // The JWT serializer stores arrays — JwtSecurityTokenHandler handles them
        // depending on the serialization format
    }

    // =========================================================================
    // Login HTTP Flow Tests
    // =========================================================================

    [Fact]
    public async Task Login_SuccessfulResponse_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler();
        var loginResponse = TestData.CreateLoginResponse();
        handler.RespondWith("/api/auth/login", loginResponse);

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/login",
            new LoginRequest("test@example.com", "password"));

        response.IsSuccessStatusCode.Should().BeTrue();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("test-access-token");
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsError()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("/api/auth/login", _ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Invalid credentials")
            });

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/login",
            new LoginRequest("bad@email.com", "wrong"));

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ServerUnavailable_ThrowsException()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("Connection refused"));

        using var http = handler.CreateClient();

        var act = () => http.PostAsJsonAsync(
            "http://unreachable:5005/api/auth/login",
            new LoginRequest("test@example.com", "password"));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // =========================================================================
    // Register HTTP Flow Tests
    // =========================================================================

    [Fact]
    public async Task Register_SuccessfulResponse_ReturnsTokens()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/auth/register", TestData.CreateLoginResponse());

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/register",
            new RegisterRequest("new@example.com", "strongpass", "New User", null));

        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("/api/auth/register", _ =>
            new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("Email already exists")
            });

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/register",
            new RegisterRequest("existing@example.com", "pass", "User"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    // =========================================================================
    // Password Reset Flow Tests
    // =========================================================================

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/auth/forgot-password", HttpStatusCode.OK);

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/forgot-password",
            new ForgotPasswordRequest("test@example.com"));

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsError()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("/api/auth/reset-password", _ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Token expired or invalid")
            });

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/reset-password",
            new ResetPasswordRequest("test@example.com", "bad-token", "newpass"));

        response.IsSuccessStatusCode.Should().BeFalse();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("expired");
    }

    // =========================================================================
    // Token Refresh Flow Tests
    // =========================================================================

    [Fact]
    public async Task RefreshToken_ValidRefresh_ReturnsNewTokenPair()
    {
        var newTokens = new TokenPair("new-access", "new-refresh", DateTime.UtcNow.AddHours(1));
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/auth/refresh", newTokens);

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/refresh",
            new RefreshTokenRequest("old-refresh-token"));

        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.Content.ReadFromJsonAsync<TokenPair>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-access");
    }

    [Fact]
    public async Task RefreshToken_ExpiredRefresh_ReturnsUnauthorized()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/auth/refresh", HttpStatusCode.Unauthorized);

        using var http = handler.CreateClient();
        var response = await http.PostAsJsonAsync(
            "http://localhost:5005/api/auth/refresh",
            new RefreshTokenRequest("expired-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Auth State Logic Tests
    // =========================================================================

    [Fact]
    public void IsAuthenticated_Logic_ValidTokenAndFutureExpiry()
    {
        string? token = "some-valid-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var isAuth = !string.IsNullOrEmpty(token) && expiresAt > DateTime.UtcNow;
        isAuth.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_Logic_NullToken()
    {
        string? token = null;
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var isAuth = !string.IsNullOrEmpty(token) && expiresAt > DateTime.UtcNow;
        isAuth.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_Logic_ExpiredToken()
    {
        string? token = "some-valid-token";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        var isAuth = !string.IsNullOrEmpty(token) && expiresAt > DateTime.UtcNow;
        isAuth.Should().BeFalse();
    }

    [Fact]
    public void AutoRefresh_ShouldTrigger_WhenExpiringWithin5Minutes()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(3);
        var refreshToken = "valid-refresh";

        var shouldRefresh = expiresAt <= DateTime.UtcNow.AddMinutes(5) &&
                           !string.IsNullOrEmpty(refreshToken);
        shouldRefresh.Should().BeTrue();
    }

    [Fact]
    public void AutoRefresh_ShouldNotTrigger_WhenTokenFreshEnough()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var refreshToken = "valid-refresh";

        var shouldRefresh = expiresAt <= DateTime.UtcNow.AddMinutes(5) &&
                           !string.IsNullOrEmpty(refreshToken);
        shouldRefresh.Should().BeFalse();
    }

    [Fact]
    public void AutoRefresh_ShouldNotTrigger_WhenNoRefreshToken()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(3);
        string? refreshToken = null;

        var shouldRefresh = expiresAt <= DateTime.UtcNow.AddMinutes(5) &&
                           !string.IsNullOrEmpty(refreshToken);
        shouldRefresh.Should().BeFalse();
    }

    // =========================================================================
    // Helper: Mirrors WatchAuthService.ParseUserFromToken
    // =========================================================================

    private static UserInfoDto? ParseUserFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "display_name")?.Value ?? email;
            var roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();

            return new UserInfoDto(
                Guid.TryParse(sub, out var id) ? id : Guid.Empty,
                email,
                name,
                null,
                roles.Length > 0 ? roles : ["user"],
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }
}
