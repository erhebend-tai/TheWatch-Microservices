using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Notifications;

/// <summary>
/// Firebase Cloud Messaging implementation of INotificationService.
/// </summary>
public class FirebaseNotificationService : INotificationService
{
    private readonly ILogger<FirebaseNotificationService> _logger;

    public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<NotificationResult> SendToDeviceAsync(
        string deviceToken, NotificationMessage message, CancellationToken ct = default)
    {
        var fcmMessage = BuildMessage(message);
        fcmMessage.Token = deviceToken;

        try
        {
            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage, ct);
            _logger.LogInformation("FCM sent to device: {MessageId}", messageId);
            return new NotificationResult(true, messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "FCM send failed for device token {Token}", deviceToken);
            return new NotificationResult(false, Error: ex.Message);
        }
    }

    public async Task<NotificationResult> SendToTopicAsync(
        string topic, NotificationMessage message, CancellationToken ct = default)
    {
        var fcmMessage = BuildMessage(message);
        fcmMessage.Topic = topic;

        try
        {
            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage, ct);
            _logger.LogInformation("FCM sent to topic '{Topic}': {MessageId}", topic, messageId);
            return new NotificationResult(true, messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "FCM send to topic '{Topic}' failed", topic);
            return new NotificationResult(false, Error: ex.Message);
        }
    }

    public async Task<List<NotificationResult>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens, NotificationMessage message, CancellationToken ct = default)
    {
        var tokens = deviceTokens.ToList();
        if (tokens.Count == 0)
            return [];

        var multicast = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = message.Title,
                Body = message.Body,
                ImageUrl = message.ImageUrl
            },
            Data = message.Data.Count > 0 ? message.Data : null,
            Android = BuildAndroidConfig(message),
            Apns = BuildApnsConfig(message)
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicast, ct);
            _logger.LogInformation("FCM multicast: {Success}/{Total} succeeded",
                response.SuccessCount, tokens.Count);

            return response.Responses.Select(r =>
                new NotificationResult(r.IsSuccess, r.MessageId,
                    r.Exception?.Message)).ToList();
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "FCM multicast failed for {Count} tokens", tokens.Count);
            return tokens.Select(_ => new NotificationResult(false, Error: ex.Message)).ToList();
        }
    }

    public async Task SubscribeToTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SubscribeToTopicAsync([deviceToken], topic);
            _logger.LogInformation("Subscribed to topic '{Topic}': {Success} success, {Failure} failure",
                topic, response.SuccessCount, response.FailureCount);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Subscribe to topic '{Topic}' failed", topic);
        }
    }

    public async Task UnsubscribeFromTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .UnsubscribeFromTopicAsync([deviceToken], topic);
            _logger.LogInformation("Unsubscribed from topic '{Topic}': {Success} success, {Failure} failure",
                topic, response.SuccessCount, response.FailureCount);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Unsubscribe from topic '{Topic}' failed", topic);
        }
    }

    private static Message BuildMessage(NotificationMessage message)
    {
        return new Message
        {
            Notification = new Notification
            {
                Title = message.Title,
                Body = message.Body,
                ImageUrl = message.ImageUrl
            },
            Data = message.Data.Count > 0 ? message.Data : null,
            Android = BuildAndroidConfig(message),
            Apns = BuildApnsConfig(message)
        };
    }

    private static AndroidConfig BuildAndroidConfig(NotificationMessage message)
    {
        var priority = message.Priority switch
        {
            NotificationPriority.Critical or NotificationPriority.High => Priority.High,
            _ => Priority.Normal
        };

        return new AndroidConfig
        {
            Priority = priority,
            Notification = new AndroidNotification
            {
                ChannelId = message.Channel.ToString().ToLowerInvariant(),
                // AndroidNotificationPriority set via channel importance instead
                DefaultSound = true,
                DefaultVibrateTimings = message.Priority >= NotificationPriority.High
            }
        };
    }

    private static ApnsConfig BuildApnsConfig(NotificationMessage message)
    {
        var interruptionLevel = message.Priority switch
        {
            NotificationPriority.Critical => "critical",
            NotificationPriority.High => "time-sensitive",
            NotificationPriority.Normal => "active",
            _ => "passive"
        };

        return new ApnsConfig
        {
            Headers = new Dictionary<string, string>
            {
                ["apns-priority"] = message.Priority >= NotificationPriority.High ? "10" : "5"
            },
            Aps = new Aps
            {
                Sound = "default",
                MutableContent = true,
                CustomData = new Dictionary<string, object>
                {
                    ["interruption-level"] = interruptionLevel
                }
            }
        };
    }
}

/// <summary>
/// No-op implementation for development/testing when Firebase is not configured.
/// </summary>
public class NoOpNotificationService : INotificationService
{
    private readonly ILogger<NoOpNotificationService> _logger;

    public NoOpNotificationService(ILogger<NoOpNotificationService> logger)
    {
        _logger = logger;
    }

    public Task<NotificationResult> SendToDeviceAsync(
        string deviceToken, NotificationMessage message, CancellationToken ct = default)
    {
        _logger.LogDebug("[NoOp] Would send to device {Token}: {Title}", deviceToken, message.Title);
        return Task.FromResult(new NotificationResult(true, "noop-" + Guid.NewGuid()));
    }

    public Task<NotificationResult> SendToTopicAsync(
        string topic, NotificationMessage message, CancellationToken ct = default)
    {
        _logger.LogDebug("[NoOp] Would send to topic '{Topic}': {Title}", topic, message.Title);
        return Task.FromResult(new NotificationResult(true, "noop-" + Guid.NewGuid()));
    }

    public Task<List<NotificationResult>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens, NotificationMessage message, CancellationToken ct = default)
    {
        var tokens = deviceTokens.ToList();
        _logger.LogDebug("[NoOp] Would send to {Count} devices: {Title}", tokens.Count, message.Title);
        return Task.FromResult(tokens.Select(_ =>
            new NotificationResult(true, "noop-" + Guid.NewGuid())).ToList());
    }

    public Task SubscribeToTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        _logger.LogDebug("[NoOp] Would subscribe {Token} to '{Topic}'", deviceToken, topic);
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        _logger.LogDebug("[NoOp] Would unsubscribe {Token} from '{Topic}'", deviceToken, topic);
        return Task.CompletedTask;
    }
}
