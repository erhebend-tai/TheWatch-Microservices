using System.Collections.Concurrent;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Services;

public interface INotificationService
{
    Task<MeshMessage> SendAsync(SendMessageRequest request);
    Task<List<MeshMessage>> GetMessagesAsync(Guid recipientId, int limit = 50);
    Task<NotificationChannel> CreateChannelAsync(CreateChannelRequest request);
    Task<List<NotificationChannel>> ListChannelsAsync();
    Task<bool> SubscribeAsync(Guid channelId, Guid nodeId);
}

public class NotificationService : INotificationService
{
    private readonly ConcurrentDictionary<Guid, MeshMessage> _messages = new();
    private readonly ConcurrentDictionary<Guid, NotificationChannel> _channels = new();

    public Task<MeshMessage> SendAsync(SendMessageRequest request)
    {
        var msg = new MeshMessage
        {
            SenderId = request.SenderId,
            RecipientId = request.RecipientId,
            ChannelId = request.ChannelId,
            Content = request.Content,
            Priority = request.Priority
        };

        _messages[msg.Id] = msg;
        return Task.FromResult(msg);
    }

    public Task<List<MeshMessage>> GetMessagesAsync(Guid recipientId, int limit)
    {
        var result = _messages.Values
            .Where(m => m.RecipientId == recipientId || m.ChannelId.HasValue)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<NotificationChannel> CreateChannelAsync(CreateChannelRequest request)
    {
        var channel = new NotificationChannel
        {
            Name = request.Name,
            Type = request.Type
        };

        _channels[channel.Id] = channel;
        return Task.FromResult(channel);
    }

    public Task<List<NotificationChannel>> ListChannelsAsync()
    {
        return Task.FromResult(_channels.Values.OrderBy(c => c.Name).ToList());
    }

    public Task<bool> SubscribeAsync(Guid channelId, Guid nodeId)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
            return Task.FromResult(false);

        if (!channel.SubscriberIds.Contains(nodeId))
            channel.SubscriberIds.Add(nodeId);

        return Task.FromResult(true);
    }
}
