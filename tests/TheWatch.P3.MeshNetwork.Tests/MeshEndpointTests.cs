using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P3.MeshNetwork.Mesh;
using Xunit;

namespace TheWatch.P3.MeshNetwork.Tests;

public class MeshEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MeshEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<MeshNode> RegisterNodeAsync(string name = "Node-1")
    {
        var response = await _client.PostAsJsonAsync("/api/nodes",
            new RegisterNodeRequest(name, $"DEV-{Guid.NewGuid():N}", 33.45, -112.07, 85));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<MeshNode>())!;
    }

    [Fact]
    public async Task RegisterNode_ReturnsCreated()
    {
        var node = await RegisterNodeAsync("Alpha");
        node.Name.Should().Be("Alpha");
        node.Status.Should().Be(NodeStatus.Online);
    }

    [Fact]
    public async Task GetNode_ReturnsOk()
    {
        var created = await RegisterNodeAsync();
        var response = await _client.GetAsync($"/api/nodes/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListNodes_ReturnsAll()
    {
        await RegisterNodeAsync("N1");
        await RegisterNodeAsync("N2");

        var result = await _client.GetFromJsonAsync<NodeListResponse>("/api/nodes");
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateNodeStatus_ReturnsUpdated()
    {
        var node = await RegisterNodeAsync();
        var response = await _client.PutAsJsonAsync($"/api/nodes/{node.Id}/status",
            new UpdateNodeStatusRequest(NodeStatus.Relaying, 33.46, -112.08, 72));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MeshNode>();
        updated!.Status.Should().Be(NodeStatus.Relaying);
        updated.BatteryPercent.Should().Be(72);
    }

    [Fact]
    public async Task GetTopology_ReturnsNodes()
    {
        await RegisterNodeAsync("T1");
        var topology = await _client.GetFromJsonAsync<TopologyResponse>("/api/topology");
        topology!.Nodes.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SendMessage_ReturnsCreated()
    {
        var sender = await RegisterNodeAsync("Sender");
        var recipient = await RegisterNodeAsync("Recipient");

        var response = await _client.PostAsJsonAsync("/api/messages",
            new SendMessageRequest(sender.Id, "Hello via mesh!", recipient.Id, null, MessagePriority.High));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var msg = await response.Content.ReadFromJsonAsync<MeshMessage>();
        msg!.Content.Should().Be("Hello via mesh!");
    }

    [Fact]
    public async Task CreateChannel_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/channels",
            new CreateChannelRequest("Emergency Alerts", ChannelType.Emergency));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var channel = await response.Content.ReadFromJsonAsync<NotificationChannel>();
        channel!.Name.Should().Be("Emergency Alerts");
    }

    [Fact]
    public async Task SubscribeToChannel_ReturnsOk()
    {
        var node = await RegisterNodeAsync();
        var channelResp = await _client.PostAsJsonAsync("/api/channels",
            new CreateChannelRequest("Community", ChannelType.Community));
        var channel = await channelResp.Content.ReadFromJsonAsync<NotificationChannel>();

        var response = await _client.PostAsync($"/api/channels/{channel!.Id}/subscribe/{node.Id}", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
