using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for mobile auth state provider logic — ClaimsPrincipal construction,
/// claims mapping from JWT data, and unauthenticated state handling.
/// Since MobileAuthStateProvider depends on MAUI SecureStorage and AuthenticationStateProvider,
/// we test the pure claims construction logic independently.
/// </summary>
public class MobileAuthStateProviderTests
{
    // =========================================================================
    // Unauthenticated User
    // =========================================================================

    [Fact]
    public void Unauthenticated_ReturnsEmptyClaimsPrincipal()
    {
        var principal = BuildClaimsPrincipal(null);

        principal.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeFalse();
    }

    // =========================================================================
    // Authenticated User
    // =========================================================================

    [Fact]
    public void Authenticated_IdentityHasJwtAuthenticationType()
    {
        var userInfo = new AuthUserInfo
        {
            UserId = Guid.NewGuid(),
            Email = "alice@watch.com",
            DisplayName = "Alice",
            Roles = ["user"]
        };

        var principal = BuildClaimsPrincipal(userInfo);

        principal.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity.AuthenticationType.Should().Be("jwt");
    }

    // =========================================================================
    // Claims Construction
    // =========================================================================

    [Fact]
    public void Claims_NameIdentifier_EqualsUserId()
    {
        var userId = Guid.NewGuid();
        var userInfo = CreateUserInfo(userId: userId);

        var principal = BuildClaimsPrincipal(userInfo);

        var nameIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        nameIdClaim.Should().NotBeNull();
        nameIdClaim!.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public void Claims_Email_EqualsUserEmail()
    {
        var userInfo = CreateUserInfo(email: "bob@watch.com");

        var principal = BuildClaimsPrincipal(userInfo);

        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("bob@watch.com");
    }

    [Fact]
    public void Claims_Name_EqualsDisplayName()
    {
        var userInfo = CreateUserInfo(displayName: "Charlie");

        var principal = BuildClaimsPrincipal(userInfo);

        var nameClaim = principal.FindFirst(ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("Charlie");
    }

    [Fact]
    public void Claims_Roles_AddedAsRoleClaims()
    {
        var userInfo = CreateUserInfo(roles: ["admin", "responder"]);

        var principal = BuildClaimsPrincipal(userInfo);

        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain("admin");
        roleClaims.Should().Contain("responder");
    }

    [Fact]
    public void Claims_MultipleRoles_AllAppearInClaims()
    {
        var userInfo = CreateUserInfo(roles: ["admin", "responder", "medic"]);

        var principal = BuildClaimsPrincipal(userInfo);

        var roleClaims = principal.FindAll(ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(3);
    }

    [Fact]
    public void Claims_NoRoles_NoRoleClaims()
    {
        var userInfo = CreateUserInfo(roles: []);

        var principal = BuildClaimsPrincipal(userInfo);

        var roleClaims = principal.FindAll(ClaimTypes.Role).ToList();
        roleClaims.Should().BeEmpty();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static AuthUserInfo CreateUserInfo(
        Guid? userId = null,
        string email = "test@watch.com",
        string displayName = "Test User",
        string[]? roles = null)
    {
        return new AuthUserInfo
        {
            UserId = userId ?? Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Roles = roles ?? ["user"]
        };
    }

    // =========================================================================
    // Mirrors MobileAuthStateProvider.BuildClaimsPrincipal logic
    // =========================================================================

    private static ClaimsPrincipal BuildClaimsPrincipal(AuthUserInfo? userInfo)
    {
        if (userInfo is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
            new(ClaimTypes.Email, userInfo.Email),
            new(ClaimTypes.Name, userInfo.DisplayName)
        };

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }
}

/// <summary>
/// Mirror of user info used by MobileAuthStateProvider from TheWatch.Mobile.Services
/// </summary>
public class AuthUserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string[] Roles { get; set; } = [];
}
