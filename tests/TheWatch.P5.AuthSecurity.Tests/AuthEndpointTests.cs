using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheWatch.P5.AuthSecurity.Models;
using TheWatch.Shared.Auth;
using Xunit;

using LoginRequest = TheWatch.P5.AuthSecurity.Auth.LoginRequest;
using LoginResponse = TheWatch.P5.AuthSecurity.Auth.LoginResponse;
using RegisterRequest = TheWatch.P5.AuthSecurity.Auth.RegisterRequest;
using RefreshTokenRequest = TheWatch.P5.AuthSecurity.Auth.RefreshTokenRequest;
using TokenPair = TheWatch.P5.AuthSecurity.Auth.TokenPair;
using UserInfo = TheWatch.P5.AuthSecurity.Auth.UserInfo;
using AssignRoleRequest = TheWatch.P5.AuthSecurity.Auth.AssignRoleRequest;

namespace TheWatch.P5.AuthSecurity.Tests;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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

    private async Task PromoteToAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WatchUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        user.Should().NotBeNull();
        await userManager.AddToRoleAsync(user!, WatchRoles.Admin);
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
        result.User.Roles.Should().Contain(WatchRoles.Patient);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = "dup@thewatch.dev";
        await RegisterTestUserAsync(email);

        var request = new RegisterRequest(email, "AnotherPass1!", "Another User");
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
        result.MfaRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = "wrongpw@thewatch.dev";
        await RegisterTestUserAsync(email, "CorrectPass1!");

        var loginRequest = new LoginRequest(email, "WrongPass1!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest("nobody@thewatch.dev", "Whatever1!");
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
    public async Task Refresh_UsedToken_ReturnsUnauthorized_CompromiseDetection()
    {
        var registered = await RegisterTestUserAsync("doublerefresh@thewatch.dev");

        // First refresh — should succeed
        var refreshRequest = new RefreshTokenRequest(registered.RefreshToken);
        var response1 = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second refresh with same token — should fail (revoked, compromise detection)
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
    public async Task Users_WithAdminJwt_ReturnsUserList()
    {
        // Register a user and promote to admin
        var registered = await RegisterTestUserAsync("adminuser@thewatch.dev");
        await PromoteToAdminAsync(registered.User.Id);

        // Re-login to get JWT with Admin role claim
        var loginRequest = new LoginRequest("adminuser@thewatch.dev", "SecurePass123!");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var adminLogin = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin!.AccessToken);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Users_WithPatientJwt_ReturnsForbidden()
    {
        var registered = await RegisterTestUserAsync("patientuser@thewatch.dev");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RoleAssign_WithAdminJwt_AssignsRole()
    {
        // Register admin
        var admin = await RegisterTestUserAsync("roleadmin@thewatch.dev");
        await PromoteToAdminAsync(admin.User.Id);
        var adminLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("roleadmin@thewatch.dev", "SecurePass123!"));
        var adminTokens = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();

        // Register target user
        var target = await RegisterTestUserAsync("roletarget@thewatch.dev");

        // Assign Responder role
        var assignRequest = new AssignRoleRequest(target.User.Id, WatchRoles.Responder);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/roles/assign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens!.AccessToken);
        request.Content = JsonContent.Create(assignRequest);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EulaCurrent_ReturnsSeededVersion()
    {
        var response = await _client.GetAsync("/api/eula/current");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
