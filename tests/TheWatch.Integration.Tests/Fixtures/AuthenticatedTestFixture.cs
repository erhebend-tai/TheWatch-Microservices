using System.Net.Http.Headers;
using TheWatch.Contracts.AuthSecurity;
using TheWatch.Contracts.AuthSecurity.Models;
using TheWatch.Integration.Tests.Infrastructure;

namespace TheWatch.Integration.Tests.Fixtures;

/// <summary>
/// Item 252: Test fixture that registers a user via P5, logs in, and provides
/// an authenticated HttpClient for all subsequent integration test calls.
/// Implements IAsyncLifetime so xUnit runs setup before any test in the collection.
/// </summary>
public class AuthenticatedTestFixture : IAsyncLifetime
{
    private readonly HttpClient _authHttp;

    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public UserInfoDto User { get; private set; } = null!;

    // Pre-configured HttpClients for each service with auth headers
    public HttpClient AuthSecurityHttp { get; private set; } = null!;
    public HttpClient VoiceEmergencyHttp { get; private set; } = null!;
    public HttpClient FirstResponderHttp { get; private set; } = null!;
    public HttpClient FamilyHealthHttp { get; private set; } = null!;
    public HttpClient DoctorServicesHttp { get; private set; } = null!;
    public HttpClient DisasterReliefHttp { get; private set; } = null!;
    public HttpClient MeshNetworkHttp { get; private set; } = null!;
    public HttpClient SurveillanceHttp { get; private set; } = null!;
    public HttpClient GeospatialHttp { get; private set; } = null!;

    public static readonly string TestEmail = $"integration-{Guid.NewGuid():N}@thewatch.test";
    public const string TestPassword = "W@tch!Integr4tion#2024Test";
    public const string TestDisplayName = "Integration Test User";

    public AuthenticatedTestFixture()
    {
        _authHttp = new HttpClient { BaseAddress = new Uri(ServiceEndpoints.AuthSecurity) };
    }

    public async Task InitializeAsync()
    {
        var authClient = new AuthSecurityClient(_authHttp);

        // Step 1: Register a test user
        User = await authClient.RegisterAsync(new RegisterRequest(
            TestEmail, TestPassword, TestDisplayName, "+15551234567"));

        // Step 2: Login to get JWT tokens
        var loginResult = await authClient.LoginAsync(new LoginRequest(TestEmail, TestPassword));
        AccessToken = loginResult.AccessToken;
        RefreshToken = loginResult.RefreshToken;
        User = loginResult.User;

        // Step 3: Create authenticated HttpClients for each service
        AuthSecurityHttp = CreateAuthenticatedClient(ServiceEndpoints.AuthSecurity);
        VoiceEmergencyHttp = CreateAuthenticatedClient(ServiceEndpoints.VoiceEmergency);
        FirstResponderHttp = CreateAuthenticatedClient(ServiceEndpoints.FirstResponder);
        FamilyHealthHttp = CreateAuthenticatedClient(ServiceEndpoints.FamilyHealth);
        DoctorServicesHttp = CreateAuthenticatedClient(ServiceEndpoints.DoctorServices);
        DisasterReliefHttp = CreateAuthenticatedClient(ServiceEndpoints.DisasterRelief);
        MeshNetworkHttp = CreateAuthenticatedClient(ServiceEndpoints.MeshNetwork);
        SurveillanceHttp = CreateAuthenticatedClient(ServiceEndpoints.Surveillance);
        GeospatialHttp = CreateAuthenticatedClient(ServiceEndpoints.Geospatial);
    }

    public Task DisposeAsync()
    {
        _authHttp.Dispose();
        AuthSecurityHttp?.Dispose();
        VoiceEmergencyHttp?.Dispose();
        FirstResponderHttp?.Dispose();
        FamilyHealthHttp?.Dispose();
        DoctorServicesHttp?.Dispose();
        DisasterReliefHttp?.Dispose();
        MeshNetworkHttp?.Dispose();
        SurveillanceHttp?.Dispose();
        GeospatialHttp?.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient CreateAuthenticatedClient(string baseUrl)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccessToken);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString("D"));
        return client;
    }
}

/// <summary>
/// xUnit collection definition to share the authenticated fixture across test classes.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection :
    ICollectionFixture<DockerComposeFixture>,
    ICollectionFixture<AuthenticatedTestFixture>
{
}
