using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.MeshNetwork.Models;

namespace TheWatch.Contracts.MeshNetwork;

public class MeshNetworkClient(HttpClient http) : ServiceClientBase(http, "MeshNetwork"), IMeshNetworkClient
{
    public Task<MeshNodeDto> GetNodeAsync(Guid id, CancellationToken ct)
        => GetAsync<MeshNodeDto>($"/api/nodes/{id}", ct);

    public Task<NodeListResponse> ListNodesAsync(CancellationToken ct)
        => GetAsync<NodeListResponse>("/api/nodes", ct);

    public Task<MeshNodeDto> RegisterNodeAsync(RegisterNodeRequest request, CancellationToken ct)
        => PostAsync<MeshNodeDto>("/api/nodes", request, ct);

    public Task<MeshNodeDto> UpdateNodeStatusAsync(Guid id, UpdateNodeStatusRequest request, CancellationToken ct)
        => PutAsync<MeshNodeDto>($"/api/nodes/{id}/status", request, ct);

    public Task DeleteNodeAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"/api/nodes/{id}", ct);

    public Task<MeshMessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken ct)
        => PostAsync<MeshMessageDto>("/api/messages", request, ct);

    public Task<List<MeshMessageDto>> GetMessagesAsync(Guid? channelId, CancellationToken ct)
    {
        var query = channelId.HasValue ? $"/api/messages?channelId={channelId}" : "/api/messages";
        return GetAsync<List<MeshMessageDto>>(query, ct);
    }

    public Task<NotificationChannelDto> CreateChannelAsync(CreateChannelRequest request, CancellationToken ct)
        => PostAsync<NotificationChannelDto>("/api/channels", request, ct);

    public Task<List<NotificationChannelDto>> ListChannelsAsync(CancellationToken ct)
        => GetAsync<List<NotificationChannelDto>>("/api/channels", ct);

    public Task<TopologyResponse> GetTopologyAsync(CancellationToken ct)
        => GetAsync<TopologyResponse>("/api/topology", ct);
}
