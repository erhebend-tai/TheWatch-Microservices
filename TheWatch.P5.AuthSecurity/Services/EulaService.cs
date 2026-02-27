using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

public class EulaService
{
    private readonly AuthIdentityDbContext _db;
    private readonly UserManager<WatchUser> _userManager;

    public EulaService(AuthIdentityDbContext db, UserManager<WatchUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<EulaVersion?> GetCurrentVersionAsync()
    {
        return await _db.EulaVersions.FirstOrDefaultAsync(v => v.IsCurrent);
    }

    public async Task AcceptCurrentVersionAsync(Guid userId, string? ipAddress)
    {
        var current = await _db.EulaVersions.FirstOrDefaultAsync(v => v.IsCurrent)
            ?? throw new InvalidOperationException("No current EULA version.");

        var existing = await _db.EulaAcceptances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.EulaVersionId == current.Id);

        if (existing is null)
        {
            _db.EulaAcceptances.Add(new EulaAcceptance
            {
                UserId = userId,
                EulaVersionId = current.Id,
                IpAddress = ipAddress
            });
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.AcceptedEulaVersion = current.Version;
            user.EulaAcceptedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<object> GetAcceptanceStatusAsync(Guid userId)
    {
        var current = await _db.EulaVersions.FirstOrDefaultAsync(v => v.IsCurrent);
        var user = await _userManager.FindByIdAsync(userId.ToString());

        return new
        {
            currentVersion = current?.Version,
            acceptedVersion = user?.AcceptedEulaVersion,
            isAccepted = current is not null && user?.AcceptedEulaVersion == current.Version,
            acceptedAt = user?.EulaAcceptedAt
        };
    }

    public async Task<EulaVersion> PublishVersionAsync(string version, string content)
    {
        // Mark all existing versions as no longer current
        var currentVersions = await _db.EulaVersions.Where(v => v.IsCurrent).ToListAsync();
        foreach (var v in currentVersions)
            v.IsCurrent = false;

        var newVersion = new EulaVersion
        {
            Version = version,
            Content = content,
            IsCurrent = true
        };
        _db.EulaVersions.Add(newVersion);
        await _db.SaveChangesAsync();
        return newVersion;
    }
}
