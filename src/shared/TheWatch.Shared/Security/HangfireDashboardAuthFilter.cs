using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace TheWatch.Shared.Security;

/// <summary>
/// Security+ 1.3: Authorization filter for Hangfire Dashboard.
/// Requires Admin role in non-Development environments.
/// Dashboard is always read-only to prevent job manipulation.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow unrestricted access in Development for debugging
        var env = httpContext.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
        if (env?.IsDevelopment() == true)
            return true;

        // Require authenticated user with Admin role
        var user = httpContext.User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
