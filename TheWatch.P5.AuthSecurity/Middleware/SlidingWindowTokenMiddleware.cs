using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TheWatch.P5.AuthSecurity.Middleware;

/// <summary>
/// JWT sliding window: if token is past 50% of its lifetime, issues a refreshed token in X-New-Token header.
/// </summary>
public class SlidingWindowTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public SlidingWindowTokenMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var enabled = _config.GetValue("Jwt:SlidingWindowEnabled", true);
        if (!enabled) return;

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true) return;

        var expClaim = user.FindFirstValue("exp");
        var iatClaim = user.FindFirstValue("iat");
        if (expClaim is null || iatClaim is null) return;

        if (!long.TryParse(expClaim, out var exp) || !long.TryParse(iatClaim, out var iat)) return;

        var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
        var iatTime = DateTimeOffset.FromUnixTimeSeconds(iat);
        var lifetime = expTime - iatTime;
        var elapsed = DateTimeOffset.UtcNow - iatTime;

        // If past 50% lifetime, issue new token
        if (elapsed > lifetime * 0.5 && elapsed < lifetime)
        {
            var newToken = GenerateRefreshedToken(user, lifetime);
            if (newToken is not null)
                context.Response.Headers["X-New-Token"] = newToken;
        }
    }

    private string? GenerateRefreshedToken(ClaimsPrincipal user, TimeSpan lifetime)
    {
        try
        {
            var jwtKey = _config["Jwt:Key"] ?? "TheWatch-P5-AuthSecurity-DevKey-Min32Chars!!";
            var issuer = _config["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
            var audience = _config["Jwt:Audience"] ?? "TheWatch";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Copy existing claims, update iat and jti
            var claims = user.Claims
                .Where(c => c.Type != "iat" && c.Type != "jti" && c.Type != "exp" && c.Type != "nbf")
                .ToList();
            claims.Add(new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));
            claims.Add(new Claim("jti", Guid.NewGuid().ToString()));

            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.Add(lifetime), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch
        {
            return null;
        }
    }
}
