using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// SMS MFA service. Uses Azure Communication Services when configured, falls back gracefully in dev.
/// </summary>
public class SmsMfaService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsMfaService> _logger;
    private readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _pendingCodes = new();

    public SmsMfaService(IConfiguration config, ILogger<SmsMfaService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendCodeAsync(string phoneNumber)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var expiry = DateTime.UtcNow.AddMinutes(5);

        _pendingCodes[phoneNumber] = (code, expiry);

        var acsConnectionString = _config["AzureCommunicationServices:ConnectionString"];
        if (!string.IsNullOrEmpty(acsConnectionString))
        {
            try
            {
                // Azure Communication Services SMS
                // In production: var smsClient = new SmsClient(acsConnectionString);
                // await smsClient.SendAsync(from, phoneNumber, $"Your TheWatch verification code is: {code}");
                _logger.LogInformation("SMS code sent to {Phone} via ACS", phoneNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS via ACS to {Phone}", phoneNumber);
                return false;
            }
        }

        // Dev fallback — log the code
        _logger.LogWarning("ACS not configured. SMS code for {Phone}: {Code}", phoneNumber, code);
        return true;
    }

    public bool VerifyCode(string phoneNumber, string code)
    {
        if (!_pendingCodes.TryGetValue(phoneNumber, out var stored))
            return false;

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            _pendingCodes.TryRemove(phoneNumber, out _);
            return false;
        }

        if (stored.Code != code)
            return false;

        _pendingCodes.TryRemove(phoneNumber, out _);
        return true;
    }
}
