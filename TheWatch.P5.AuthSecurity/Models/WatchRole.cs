using Microsoft.AspNetCore.Identity;

namespace TheWatch.P5.AuthSecurity.Models;

public class WatchRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public int RoleLevel { get; set; }
    public bool IsSystemRole { get; set; }
    public int MaxSessionDurationMinutes { get; set; } = 480;
    public bool RequiresMfa { get; set; }
    public Guid? ParentRoleId { get; set; }
    public string? AllowedIpRanges { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public WatchRole() { }

    public WatchRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
