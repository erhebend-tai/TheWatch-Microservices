using TheWatch.Contracts.MeshNetwork.Models;

namespace TheWatch.Contracts.MeshNetwork;

public interface IMeshNetworkClient
{
    Task<MeshNodeDto> GetNodeAsync(Guid id, CancellationToken ct = default);
    Task<NodeListResponse> ListNodesAsync(CancellationToken ct = default);
    Task<MeshNodeDto> RegisterNodeAsync(RegisterNodeRequest request, CancellationToken ct = default);
    Task<MeshNodeDto> UpdateNodeStatusAsync(Guid id, UpdateNodeStatusRequest request, CancellationToken ct = default);
    Task DeleteNodeAsync(Guid id, CancellationToken ct = default);
    Task<MeshMessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken ct = default);
    Task<List<MeshMessageDto>> GetMessagesAsync(Guid? channelId = null, CancellationToken ct = default);
    Task<NotificationChannelDto> CreateChannelAsync(CreateChannelRequest request, CancellationToken ct = default);
    Task<List<NotificationChannelDto>> ListChannelsAsync(CancellationToken ct = default);
    Task<TopologyResponse> GetTopologyAsync(CancellationToken ct = default);
}
