using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<MeshMessage> _messages;
    private readonly IWatchRepository<NotificationChannel> _channels;

    public NotificationService(IWatchRepository<MeshMessage> messages, IWatchRepository<NotificationChannel> channels)
    {
        _messages = messages;
        _channels = channels;
    }

    public async Task<MeshMessage> SendAsync(SendMessageRequest request)
    {
        var msg = new MeshMessage
        {
            SenderId = request.SenderId,
            RecipientId = request.RecipientId,
            ChannelId = request.ChannelId,
            Content = request.Content,
            Priority = request.Priority
        };

        return await _messages.AddAsync(msg);
    }

    public async Task<List<MeshMessage>> GetMessagesAsync(Guid recipientId, int limit)
    {
        return await _messages.Query()
            .Where(m => m.RecipientId == recipientId || m.ChannelId.HasValue)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<NotificationChannel> CreateChannelAsync(CreateChannelRequest request)
    {
        var channel = new NotificationChannel
        {
            Name = request.Name,
            Type = request.Type
        };

        return await _channels.AddAsync(channel);
    }

    public async Task<List<NotificationChannel>> ListChannelsAsync()
    {
        return await _channels.Query().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<bool> SubscribeAsync(Guid channelId, Guid nodeId)
    {
        var channel = await _channels.GetByIdAsync(channelId);
        if (channel is null) return false;

        if (!channel.SubscriberIds.Contains(nodeId))
            channel.SubscriberIds.Add(nodeId);

        await _channels.UpdateAsync(channel);
        return true;
    }
}
