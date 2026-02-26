using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, CheckIn> _checkIns = new();

    public Task<CheckIn> CreateAsync(Guid memberId, CreateCheckInRequest request)
    {
        var checkIn = new CheckIn
        {
            MemberId = memberId,
            Status = request.Status,
            Message = request.Message,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _checkIns[checkIn.Id] = checkIn;
        return Task.FromResult(checkIn);
    }

    public Task<List<CheckIn>> GetForMemberAsync(Guid memberId, int limit)
    {
        var result = _checkIns.Values
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<CheckIn?> GetLatestForMemberAsync(Guid memberId)
    {
        var latest = _checkIns.Values
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task CleanupOldCheckInsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = _checkIns.Values.Where(c => c.Timestamp < cutoff).Select(c => c.Id).ToList();
        foreach (var id in old)
            _checkIns.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
