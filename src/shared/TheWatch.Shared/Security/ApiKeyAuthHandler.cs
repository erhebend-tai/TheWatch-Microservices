using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheWatch.Shared.Security;

/// <summary>
/// Custom authentication handler for inter-service API key authentication.
/// Services authenticate to each other via pre-shared keys in the X-Api-Key header.
/// Uses constant-time comparison to prevent timing attacks.
/// </summary>
public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly IConfiguration _config;

    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var expectedKey = _config["Security:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
        {
            // Security+ 1.2: Reject all API key auth when no key is configured (no dev bypass)
            Logger.LogWarning("[SEC:APIKEY_NOCFG] API key authentication attempted but Security:ApiKey is not configured");
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is not configured."));
        }

        // Security+ 1.2: Use constant-time comparison to prevent timing attacks
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        if (!CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            Logger.LogWarning("[SEC:APIKEY_INVALID] Invalid API key from {IP}",
                Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "service-account"),
            new Claim(ClaimTypes.Name, "ServiceAccount"),
            new Claim(ClaimTypes.Role, "ServiceAccount"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("auth_method", "api_key")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
