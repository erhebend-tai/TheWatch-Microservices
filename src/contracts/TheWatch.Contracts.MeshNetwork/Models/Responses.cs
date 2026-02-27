namespace TheWatch.Contracts.MeshNetwork.Models;

public record NodeListResponse(List<MeshNodeDto> Items, int TotalCount);
public record TopologyResponse(List<MeshNodeDto> Nodes, List<PeerConnection> Connections);
public record PeerConnection(Guid NodeA, Guid NodeB);
