using Microsoft.AspNetCore.Identity;

namespace TheWatch.P5.AuthSecurity.Models;

public class WatchRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public WatchRole() { }

    public WatchRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
