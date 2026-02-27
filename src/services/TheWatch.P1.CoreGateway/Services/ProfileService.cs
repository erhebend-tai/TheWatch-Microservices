using Microsoft.EntityFrameworkCore;
using TheWatch.P1.CoreGateway.Core;

namespace TheWatch.P1.CoreGateway.Services;

public interface IProfileService
{
    Task<UserProfile> CreateAsync(CreateProfileRequest request);
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<ProfileListResponse> ListAsync(int page = 1, int pageSize = 20, UserRole? role = null);
    Task<UserProfile?> UpdateAsync(Guid id, UpdateProfileRequest request);
    Task<UserProfile?> SetPreferenceAsync(Guid id, SetPreferenceRequest request);
    Task<bool> DeactivateAsync(Guid id);
}

public class ProfileService : IProfileService
{
    private readonly IWatchRepository<UserProfile> _profiles;

    public ProfileService(IWatchRepository<UserProfile> profiles)
    {
        _profiles = profiles;
    }

    public async Task<UserProfile> CreateAsync(CreateProfileRequest request)
    {
        var profile = new UserProfile
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role
        };

        return await _profiles.AddAsync(profile);
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        return await _profiles.GetByIdAsync(id);
    }

    public async Task<ProfileListResponse> ListAsync(int page, int pageSize, UserRole? role)
    {
        var query = _profiles.Query().Where(p => p.IsActive);

        if (role.HasValue)
            query = query.Where(p => p.Role == role.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ProfileListResponse(items, total, page, pageSize);
    }

    public async Task<UserProfile?> UpdateAsync(Guid id, UpdateProfileRequest request)
    {
        var profile = await _profiles.GetByIdAsync(id);
        if (profile is null) return null;

        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName;
        if (request.Phone is not null) profile.Phone = request.Phone;
        if (request.Latitude.HasValue) profile.Latitude = request.Latitude;
        if (request.Longitude.HasValue) profile.Longitude = request.Longitude;
        profile.UpdatedAt = DateTime.UtcNow;

        await _profiles.UpdateAsync(profile);
        return profile;
    }

    public async Task<UserProfile?> SetPreferenceAsync(Guid id, SetPreferenceRequest request)
    {
        var profile = await _profiles.GetByIdAsync(id);
        if (profile is null) return null;

        profile.Preferences[request.Key] = request.Value;
        profile.UpdatedAt = DateTime.UtcNow;

        await _profiles.UpdateAsync(profile);
        return profile;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var profile = await _profiles.GetByIdAsync(id);
        if (profile is null) return false;

        profile.IsActive = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await _profiles.UpdateAsync(profile);
        return true;
    }
}
