using Microsoft.EntityFrameworkCore;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Services;

public interface IMeshService
{
    Task<MeshNode> RegisterNodeAsync(RegisterNodeRequest request);
    Task<MeshNode?> GetNodeAsync(Guid id);
    Task<NodeListResponse> ListNodesAsync(NodeStatus? status = null);
    Task<MeshNode?> UpdateNodeStatusAsync(Guid id, UpdateNodeStatusRequest request);
    Task<TopologyResponse> GetTopologyAsync();
    Task CleanupStaleNodesAsync(TimeSpan timeout);
}

public class MeshService : IMeshService
{
    private readonly IWatchRepository<MeshNode> _nodes;

    public MeshService(IWatchRepository<MeshNode> nodes)
    {
        _nodes = nodes;
    }

    public async Task<MeshNode> RegisterNodeAsync(RegisterNodeRequest request)
    {
        var node = new MeshNode
        {
            Name = request.Name,
            DeviceId = request.DeviceId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            BatteryPercent = request.BatteryPercent,
            Status = NodeStatus.Online
        };

        return await _nodes.AddAsync(node);
    }

    public async Task<MeshNode?> GetNodeAsync(Guid id)
    {
        return await _nodes.GetByIdAsync(id);
    }

    public async Task<NodeListResponse> ListNodesAsync(NodeStatus? status)
    {
        var query = _nodes.Query();
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        var items = await query.OrderByDescending(n => n.LastSeenAt).ToListAsync();
        return new NodeListResponse(items, items.Count);
    }

    public async Task<MeshNode?> UpdateNodeStatusAsync(Guid id, UpdateNodeStatusRequest request)
    {
        var node = await _nodes.GetByIdAsync(id);
        if (node is null) return null;

        node.Status = request.Status;
        if (request.Latitude.HasValue) node.Latitude = request.Latitude;
        if (request.Longitude.HasValue) node.Longitude = request.Longitude;
        if (request.BatteryPercent.HasValue) node.BatteryPercent = request.BatteryPercent;
        node.LastSeenAt = DateTime.UtcNow;

        await _nodes.UpdateAsync(node);
        return node;
    }

    public async Task<TopologyResponse> GetTopologyAsync()
    {
        var onlineNodes = await _nodes.Query()
            .Where(n => n.Status != NodeStatus.Offline)
            .ToListAsync();

        var connections = onlineNodes
            .SelectMany(n => n.ConnectedPeers.Select(p => new PeerConnection(n.Id, p)))
            .Distinct()
            .ToList();

        return new TopologyResponse(onlineNodes, connections);
    }

    public async Task CleanupStaleNodesAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var stale = await _nodes.Query()
            .Where(n => n.LastSeenAt < cutoff && n.Status != NodeStatus.Offline)
            .ToListAsync();

        foreach (var node in stale)
        {
            node.Status = NodeStatus.Offline;
            node.ConnectedPeers.Clear();
            await _nodes.UpdateAsync(node);
        }
    }
}
