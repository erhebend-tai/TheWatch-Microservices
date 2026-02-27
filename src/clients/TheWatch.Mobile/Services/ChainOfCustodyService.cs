using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Chain-of-custody evidence integrity service.
/// Computes SHA-256 hashes, creates linked custody records with device signatures,
/// and maintains a hash chain where each record references the previous hash.
/// Uses HMAC-SHA256 with a device-local key from SecureStorage.
/// </summary>
public class ChainOfCustodyService
{
    private readonly WatchAuthService _auth;
    private readonly EmergencyLocationService _location;
    private readonly ILogger<ChainOfCustodyService> _logger;
    private readonly List<CustodyRecord> _chain = [];
    private string? _previousHash;

    public IReadOnlyList<CustodyRecord> Chain => _chain.AsReadOnly();

    public ChainOfCustodyService(
        WatchAuthService auth,
        EmergencyLocationService location,
        ILogger<ChainOfCustodyService> logger)
    {
        _auth = auth;
        _location = location;
        _logger = logger;
    }

    /// <summary>
    /// Compute SHA-256 hash of a file.
    /// </summary>
    public async Task<string> ComputeHashAsync(Stream fileStream)
    {
        var hash = await SHA256.HashDataAsync(fileStream);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Create a custody record for an evidence artifact.
    /// Each record references the previous record's hash, forming a linked chain.
    /// </summary>
    public async Task<CustodyRecord> CreateRecordAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var fileHash = await ComputeHashAsync(stream);

        var deviceId = $"{DeviceInfo.Current.Manufacturer}_{DeviceInfo.Current.Model}_{DeviceInfo.Current.Name}";
        var userId = _auth.CurrentUser?.Id ?? Guid.Empty;
        var loc = _location.LastLocation;

        var record = new CustodyRecord
        {
            Id = Guid.NewGuid(),
            FileHash = fileHash,
            FilePath = filePath,
            Timestamp = DateTime.UtcNow,
            DeviceId = deviceId,
            UserId = userId,
            Latitude = loc?.Latitude ?? 0,
            Longitude = loc?.Longitude ?? 0,
            PreviousHash = _previousHash
        };

        // Sign the record with device-local HMAC key
        var signingKey = await GetOrCreateSigningKeyAsync();
        record.Signature = ComputeSignature(record, signingKey);

        _chain.Add(record);
        _previousHash = record.FileHash;

        _logger.LogInformation("Custody record created: {Hash} for {Path}", fileHash[..16], filePath);
        return record;
    }

    /// <summary>
    /// Verify the integrity of the entire custody chain.
    /// </summary>
    public async Task<bool> VerifyChainAsync()
    {
        string? expectedPrevious = null;

        foreach (var record in _chain)
        {
            if (record.PreviousHash != expectedPrevious)
            {
                _logger.LogError("Chain broken at record {Id}: expected previous {Expected}, got {Actual}",
                    record.Id, expectedPrevious, record.PreviousHash);
                return false;
            }

            // Verify file hash still matches
            if (File.Exists(record.FilePath))
            {
                await using var stream = File.OpenRead(record.FilePath);
                var currentHash = await ComputeHashAsync(stream);
                if (currentHash != record.FileHash)
                {
                    _logger.LogError("File hash mismatch for {Path}: expected {Expected}, got {Actual}",
                        record.FilePath, record.FileHash, currentHash);
                    return false;
                }
            }

            expectedPrevious = record.FileHash;
        }

        return true;
    }

    /// <summary>Export chain as JSON for server upload.</summary>
    public string ExportChainJson()
    {
        return JsonSerializer.Serialize(_chain, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ComputeSignature(CustodyRecord record, byte[] key)
    {
        var data = $"{record.FileHash}:{record.Timestamp:O}:{record.DeviceId}:{record.UserId}:{record.PreviousHash}";
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }

    private static async Task<byte[]> GetOrCreateSigningKeyAsync()
    {
        const string keyName = "custody_signing_key";

        var existing = await SecureStorage.GetAsync(keyName);
        if (existing is not null)
        {
            return Convert.FromBase64String(existing);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        await SecureStorage.SetAsync(keyName, Convert.ToBase64String(key));
        return key;
    }
}

public class CustodyRecord
{
    public Guid Id { get; set; }
    public string FileHash { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = "";
    public Guid UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? PreviousHash { get; set; }
    public string Signature { get; set; } = "";
}
