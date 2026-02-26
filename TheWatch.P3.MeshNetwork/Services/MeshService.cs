using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, MeshNode> _nodes = new();

    public Task<MeshNode> RegisterNodeAsync(RegisterNodeRequest request)
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

        _nodes[node.Id] = node;
        return Task.FromResult(node);
    }

    public Task<MeshNode?> GetNodeAsync(Guid id)
    {
        _nodes.TryGetValue(id, out var node);
        return Task.FromResult(node);
    }

    public Task<NodeListResponse> ListNodesAsync(NodeStatus? status)
    {
        var query = _nodes.Values.AsEnumerable();
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        var items = query.OrderByDescending(n => n.LastSeenAt).ToList();
        return Task.FromResult(new NodeListResponse(items, items.Count));
    }

    public Task<MeshNode?> UpdateNodeStatusAsync(Guid id, UpdateNodeStatusRequest request)
    {
        if (!_nodes.TryGetValue(id, out var node))
            return Task.FromResult<MeshNode?>(null);

        node.Status = request.Status;
        if (request.Latitude.HasValue) node.Latitude = request.Latitude;
        if (request.Longitude.HasValue) node.Longitude = request.Longitude;
        if (request.BatteryPercent.HasValue) node.BatteryPercent = request.BatteryPercent;
        node.LastSeenAt = DateTime.UtcNow;

        return Task.FromResult<MeshNode?>(node);
    }

    public Task<TopologyResponse> GetTopologyAsync()
    {
        var onlineNodes = _nodes.Values.Where(n => n.Status != NodeStatus.Offline).ToList();
        var connections = onlineNodes
            .SelectMany(n => n.ConnectedPeers.Select(p => new PeerConnection(n.Id, p)))
            .Distinct()
            .ToList();

        return Task.FromResult(new TopologyResponse(onlineNodes, connections));
    }

    public Task CleanupStaleNodesAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        foreach (var node in _nodes.Values.Where(n => n.LastSeenAt < cutoff && n.Status != NodeStatus.Offline))
        {
            node.Status = NodeStatus.Offline;
            node.ConnectedPeers.Clear();
        }
        return Task.CompletedTask;
    }
}
