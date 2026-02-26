using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P5.AuthSecurity.Auth;
using Xunit;

namespace TheWatch.P5.AuthSecurity.Tests;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<LoginResponse> RegisterTestUserAsync(string email = "test@thewatch.dev", string password = "SecurePass123!")
    {
        var request = new RegisterRequest(email, password, "Test User", "+1555000111");
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        return result!;
    }

    [Fact]
    public async Task Register_ReturnsTokensAndUserInfo()
    {
        var result = await RegisterTestUserAsync("register@thewatch.dev");

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.User.Email.Should().Be("register@thewatch.dev");
        result.User.DisplayName.Should().Be("Test User");
        result.User.Roles.Should().Contain("user");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = "dup@thewatch.dev";
        await RegisterTestUserAsync(email);

        var request = new RegisterRequest(email, "AnotherPass!", "Another User");
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var email = "login@thewatch.dev";
        var password = "LoginPass123!";
        await RegisterTestUserAsync(email, password);

        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = "wrongpw@thewatch.dev";
        await RegisterTestUserAsync(email, "CorrectPass!");

        var loginRequest = new LoginRequest(email, "WrongPass!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest("nobody@thewatch.dev", "whatever");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokenPair()
    {
        var registered = await RegisterTestUserAsync("refresh@thewatch.dev");

        var refreshRequest = new RefreshTokenRequest(registered.RefreshToken);
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<TokenPair>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        tokens.AccessToken.Should().NotBe(registered.AccessToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var refreshRequest = new RefreshTokenRequest("invalid-token");
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UsedToken_ReturnsUnauthorized()
    {
        var registered = await RegisterTestUserAsync("doublerefresh@thewatch.dev");

        // First refresh — should succeed
        var refreshRequest = new RefreshTokenRequest(registered.RefreshToken);
        var response1 = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second refresh with same token — should fail (revoked)
        var response2 = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidJwt_ReturnsUserInfo()
    {
        var registered = await RegisterTestUserAsync("me@thewatch.dev");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserInfo>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("me@thewatch.dev");
    }

    [Fact]
    public async Task Me_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_WithValidJwt_ReturnsUserList()
    {
        var registered = await RegisterTestUserAsync("listuser@thewatch.dev");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
