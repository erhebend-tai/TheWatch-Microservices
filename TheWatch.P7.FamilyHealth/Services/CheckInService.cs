using Microsoft.EntityFrameworkCore;
using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Services;

public interface ICheckInService
{
    Task<CheckIn> CreateAsync(Guid memberId, CreateCheckInRequest request);
    Task<List<CheckIn>> GetForMemberAsync(Guid memberId, int limit = 50);
    Task<CheckIn?> GetLatestForMemberAsync(Guid memberId);
    Task CleanupOldCheckInsAsync(TimeSpan olderThan);
}

public class CheckInService : ICheckInService
{
    private readonly IWatchRepository<CheckIn> _checkIns;

    public CheckInService(IWatchRepository<CheckIn> checkIns)
    {
        _checkIns = checkIns;
    }

    public async Task<CheckIn> CreateAsync(Guid memberId, CreateCheckInRequest request)
    {
        var checkIn = new CheckIn
        {
            MemberId = memberId,
            Status = request.Status,
            Message = request.Message,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        return await _checkIns.AddAsync(checkIn);
    }

    public async Task<List<CheckIn>> GetForMemberAsync(Guid memberId, int limit)
    {
        return await _checkIns.Query()
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<CheckIn?> GetLatestForMemberAsync(Guid memberId)
    {
        return await _checkIns.Query()
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task CleanupOldCheckInsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var oldIds = await _checkIns.Query()
            .Where(c => c.Timestamp < cutoff)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var id in oldIds)
            await _checkIns.DeleteAsync(id);
    }
}
