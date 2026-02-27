using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Progressive brute force detection: doubles lockout per cycle, 3 cycles → admin intervention.
/// Works alongside Identity's built-in lockout (5 attempts → 15 min).
/// </summary>
public class BruteForceDetectionService
{
    private readonly AuthIdentityDbContext _db;
    private readonly UserManager<WatchUser> _userManager;
    private readonly ILogger<BruteForceDetectionService> _logger;

    public BruteForceDetectionService(AuthIdentityDbContext db, UserManager<WatchUser> userManager, ILogger<BruteForceDetectionService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> CheckAndEscalateAsync(Guid userId)
    {
        var recentFailures = await _db.AuditEvents
            .Where(a => a.UserId == userId && !a.IsSuccess && a.EventType == "Login")
            .Where(a => a.Timestamp > DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        if (recentFailures >= 15) // 3 lockout cycles of 5
        {
            // Deactivate account, require admin intervention
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
                _logger.LogWarning("User {UserId} deactivated due to excessive login failures ({Count})", userId, recentFailures);
            }
            return true;
        }

        // Progressive lockout doubling
        var lockoutMinutes = recentFailures switch
        {
            >= 10 => 60,
            >= 5 => 30,
            _ => 15
        };

        var user2 = await _userManager.FindByIdAsync(userId.ToString());
        if (user2 is not null)
        {
            await _userManager.SetLockoutEndDateAsync(user2, DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes));
        }

        return false;
    }
}
