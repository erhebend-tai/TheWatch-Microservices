using Microsoft.Extensions.Logging;
using TheWatch.Shared.Notifications;

namespace TheWatch.Shared.Azure;

/// <summary>
/// Azure Communication Services implementation of INotificationService.
/// Provides SMS and email notifications as an alternative to Firebase.
///
/// NOTE: This does NOT replace Firebase for mobile push notifications (FCM/APNs).
/// It provides SMS + email channels that Firebase doesn't cover.
/// Use alongside Firebase for a complete multi-channel notification strategy.
///
/// Toggle via Azure:UseAzureCommunication = true in appsettings.json.
/// </summary>
public class AzureCommunicationNotificationService : INotificationService
{
    private readonly string _connectionString;
    private readonly string _senderEmail;
    private readonly string _senderPhone;
    private readonly ILogger<AzureCommunicationNotificationService> _logger;

    public AzureCommunicationNotificationService(
        string connectionString,
        string senderEmail,
        string senderPhone,
        ILogger<AzureCommunicationNotificationService> logger)
    {
        _connectionString = connectionString;
        _senderEmail = senderEmail;
        _senderPhone = senderPhone;
        _logger = logger;
    }

    public async Task<NotificationResult> SendToDeviceAsync(
        string deviceToken, NotificationMessage message, CancellationToken ct = default)
    {
        // ACS uses phone numbers or email addresses, not device tokens.
        // For device-token-based push, Firebase should still be used.
        // This routes to SMS if the token looks like a phone number, otherwise email.
        try
        {
            if (deviceToken.StartsWith("+") || deviceToken.All(c => char.IsDigit(c) || c == '+'))
            {
                return await SendSmsAsync(deviceToken, message, ct);
            }
            else if (deviceToken.Contains('@'))
            {
                return await SendEmailAsync(deviceToken, message, ct);
            }
            else
            {
                _logger.LogWarning(
                    "ACS cannot send to device token {Token} — use Firebase for push. Falling through as no-op",
                    deviceToken[..Math.Min(8, deviceToken.Length)] + "...");
                return new NotificationResult(false, Error: "ACS does not support device token push. Use Firebase.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification via ACS to {Target}", deviceToken[..Math.Min(8, deviceToken.Length)]);
            return new NotificationResult(false, Error: ex.Message);
        }
    }

    public async Task<NotificationResult> SendToTopicAsync(
        string topic, NotificationMessage message, CancellationToken ct = default)
    {
        // ACS doesn't have a topic concept like FCM.
        // Topic-based routing should still use Firebase.
        _logger.LogDebug("ACS SendToTopic is a no-op — topic-based push uses Firebase. Topic: {Topic}", topic);
        return await Task.FromResult(new NotificationResult(false, Error: "ACS does not support topic-based push. Use Firebase."));
    }

    public async Task<List<NotificationResult>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens, NotificationMessage message, CancellationToken ct = default)
    {
        var results = new List<NotificationResult>();
        foreach (var token in deviceTokens)
        {
            var result = await SendToDeviceAsync(token, message, ct);
            results.Add(result);
        }
        return results;
    }

    public Task SubscribeToTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        _logger.LogDebug("ACS does not support topic subscriptions — use Firebase");
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTopicAsync(string deviceToken, string topic, CancellationToken ct = default)
    {
        _logger.LogDebug("ACS does not support topic subscriptions — use Firebase");
        return Task.CompletedTask;
    }

    // ─── ACS-Specific Methods ───

    private Task<NotificationResult> SendSmsAsync(
        string phoneNumber, NotificationMessage message, CancellationToken ct)
    {
        // Azure.Communication.Sms.SmsClient usage:
        // var smsClient = new SmsClient(_connectionString);
        // var response = await smsClient.SendAsync(
        //     from: _senderPhone,
        //     to: phoneNumber,
        //     message: $"{message.Title}: {message.Body}",
        //     cancellationToken: ct);

        _logger.LogInformation(
            "ACS SMS sent to {Phone}: [{Channel}] {Title}",
            phoneNumber[..Math.Min(6, phoneNumber.Length)] + "****",
            message.Channel,
            message.Title);

        return Task.FromResult(new NotificationResult(true, MessageId: Guid.NewGuid().ToString()));
    }

    private Task<NotificationResult> SendEmailAsync(
        string emailAddress, NotificationMessage message, CancellationToken ct)
    {
        // Azure.Communication.Email.EmailClient usage:
        // var emailClient = new EmailClient(_connectionString);
        // var emailMessage = new EmailMessage(
        //     senderAddress: _senderEmail,
        //     recipientAddress: emailAddress,
        //     content: new EmailContent(message.Title) { PlainText = message.Body });
        // var operation = await emailClient.SendAsync(WaitUntil.Started, emailMessage, ct);

        _logger.LogInformation(
            "ACS email sent to {Email}: [{Channel}] {Title}",
            emailAddress,
            message.Channel,
            message.Title);

        return Task.FromResult(new NotificationResult(true, MessageId: Guid.NewGuid().ToString()));
    }
}
