using System.Collections.Concurrent;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Services;

public interface IResourceService
{
    Task<ResourceItem> DonateAsync(DonateResourceRequest request);
    Task<ResourceRequest> RequestAsync(CreateResourceRequestRecord request);
    Task<List<ResourceItem>> ListResourcesAsync(ResourceCategory? category = null, Guid? disasterEventId = null);
    Task<List<ResourceRequest>> ListRequestsAsync(RequestStatus? status = null, Guid? disasterEventId = null);
    Task<int> MatchRequestsToResourcesAsync(double maxDistanceKm = 100.0);
}

public class ResourceService : IResourceService
{
    private readonly ConcurrentDictionary<Guid, ResourceItem> _resources = new();
    private readonly ConcurrentDictionary<Guid, ResourceRequest> _requests = new();

    public Task<ResourceItem> DonateAsync(DonateResourceRequest request)
    {
        var item = new ResourceItem
        {
            Category = request.Category,
            Name = request.Name,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Location = new GeoPoint(request.Latitude, request.Longitude),
            DonorId = request.DonorId,
            DisasterEventId = request.DisasterEventId
        };

        _resources[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<ResourceRequest> RequestAsync(CreateResourceRequestRecord request)
    {
        var req = new ResourceRequest
        {
            RequesterId = request.RequesterId,
            Category = request.Category,
            Quantity = request.Quantity,
            Priority = request.Priority,
            Location = new GeoPoint(request.Latitude, request.Longitude),
            DisasterEventId = request.DisasterEventId
        };

        _requests[req.Id] = req;
        return Task.FromResult(req);
    }

    public Task<List<ResourceItem>> ListResourcesAsync(ResourceCategory? category, Guid? disasterEventId)
    {
        var query = _resources.Values.AsEnumerable();

        if (category.HasValue)
            query = query.Where(r => r.Category == category.Value);
        if (disasterEventId.HasValue)
            query = query.Where(r => r.DisasterEventId == disasterEventId.Value);

        return Task.FromResult(query.OrderByDescending(r => r.CreatedAt).ToList());
    }

    public Task<List<ResourceRequest>> ListRequestsAsync(RequestStatus? status, Guid? disasterEventId)
    {
        var query = _requests.Values.AsEnumerable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (disasterEventId.HasValue)
            query = query.Where(r => r.DisasterEventId == disasterEventId.Value);

        return Task.FromResult(query.OrderByDescending(r => r.Priority).ThenBy(r => r.CreatedAt).ToList());
    }

    public Task<int> MatchRequestsToResourcesAsync(double maxDistanceKm)
    {
        var matched = 0;
        var openRequests = _requests.Values
            .Where(r => r.Status == RequestStatus.Open)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToList();

        foreach (var req in openRequests)
        {
            var available = _resources.Values
                .Where(r => r.Status == ResourceStatus.Available
                         && r.Category == req.Category
                         && r.Quantity >= req.Quantity)
                .Select(r => (Resource: r, Distance: HaversineKm(
                    req.Location.Latitude, req.Location.Longitude,
                    r.Location.Latitude, r.Location.Longitude)))
                .Where(x => x.Distance <= maxDistanceKm)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (available.Resource is not null)
            {
                req.Status = RequestStatus.Matched;
                req.MatchedResourceId = available.Resource.Id;
                available.Resource.Status = ResourceStatus.Reserved;
                matched++;
            }
        }

        return Task.FromResult(matched);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
