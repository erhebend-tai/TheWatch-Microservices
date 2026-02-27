using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for chain-of-custody cryptographic operations.
/// Reimplements the pure crypto algorithms from ChainOfCustodyService:
/// SHA-256 hashing, HMAC-SHA256 signing, and hash chain verification.
/// </summary>
public class ChainOfCustodyTests
{
    // =========================================================================
    // SHA-256 Hashing
    // =========================================================================

    [Fact]
    public async Task ComputeHashAsync_ReturnsConsistentHash()
    {
        var data = Encoding.UTF8.GetBytes("test evidence data");
        using var stream1 = new MemoryStream(data);
        using var stream2 = new MemoryStream(data);

        var hash1 = await ComputeHashAsync(stream1);
        var hash2 = await ComputeHashAsync(stream2);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task ComputeHashAsync_ReturnsLowercaseHex()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        var hash = await ComputeHashAsync(stream);

        hash.Should().MatchRegex("^[0-9a-f]{64}$"); // SHA-256 = 64 hex chars
    }

    [Fact]
    public async Task ComputeHashAsync_DifferentData_DifferentHashes()
    {
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("data 1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("data 2"));

        var hash1 = await ComputeHashAsync(stream1);
        var hash2 = await ComputeHashAsync(stream2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task ComputeHashAsync_EmptyStream_ReturnsHash()
    {
        using var stream = new MemoryStream([]);

        var hash = await ComputeHashAsync(stream);

        // SHA-256 of empty input is well-known
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public async Task ComputeHashAsync_LargeData_ProducesHash()
    {
        var largeData = new byte[1024 * 1024]; // 1 MB
        Random.Shared.NextBytes(largeData);
        using var stream = new MemoryStream(largeData);

        var hash = await ComputeHashAsync(stream);

        hash.Should().HaveLength(64);
    }

    // =========================================================================
    // HMAC-SHA256 Signing
    // =========================================================================

    [Fact]
    public void ComputeSignature_ReturnsConsistentSignature()
    {
        var record = CreateTestRecord();
        var key = RandomNumberGenerator.GetBytes(32);

        var sig1 = ComputeSignature(record, key);
        var sig2 = ComputeSignature(record, key);

        sig1.Should().Be(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentKeys_DifferentSignatures()
    {
        var record = CreateTestRecord();
        var key1 = RandomNumberGenerator.GetBytes(32);
        var key2 = RandomNumberGenerator.GetBytes(32);

        var sig1 = ComputeSignature(record, key1);
        var sig2 = ComputeSignature(record, key2);

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentData_DifferentSignatures()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var record1 = CreateTestRecord();
        var record2 = CreateTestRecord();
        record2.FileHash = "different_hash_value";

        var sig1 = ComputeSignature(record1, key);
        var sig2 = ComputeSignature(record2, key);

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_IncludesAllFields()
    {
        var record = CreateTestRecord();
        var key = RandomNumberGenerator.GetBytes(32);

        // Change each field and verify signature changes
        var baseSig = ComputeSignature(record, key);

        var r2 = CreateTestRecord();
        r2.Timestamp = record.Timestamp.AddSeconds(1);
        ComputeSignature(r2, key).Should().NotBe(baseSig);

        var r3 = CreateTestRecord();
        r3.DeviceId = "different-device";
        ComputeSignature(r3, key).Should().NotBe(baseSig);

        var r4 = CreateTestRecord();
        r4.UserId = Guid.NewGuid();
        ComputeSignature(r4, key).Should().NotBe(baseSig);
    }

    [Fact]
    public void ComputeSignature_ReturnsLowercaseHex()
    {
        var record = CreateTestRecord();
        var key = RandomNumberGenerator.GetBytes(32);

        var sig = ComputeSignature(record, key);

        sig.Should().MatchRegex("^[0-9a-f]{64}$"); // HMAC-SHA256 = 64 hex chars
    }

    // =========================================================================
    // Hash Chain Verification
    // =========================================================================

    [Fact]
    public void VerifyChain_ValidChain_ReturnsTrue()
    {
        var chain = BuildChain(3);

        var isValid = VerifyChainLinks(chain);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChain_EmptyChain_ReturnsTrue()
    {
        var chain = new List<CustodyRecord>();

        var isValid = VerifyChainLinks(chain);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChain_SingleRecord_ReturnsTrue()
    {
        var chain = BuildChain(1);

        var isValid = VerifyChainLinks(chain);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChain_BrokenLink_ReturnsFalse()
    {
        var chain = BuildChain(3);
        // Tamper with middle record's PreviousHash
        chain[1].PreviousHash = "tampered_hash_value";

        var isValid = VerifyChainLinks(chain);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyChain_TamperedFileHash_DetectsManipulation()
    {
        var chain = BuildChain(3);
        // Tamper with first record's FileHash
        chain[0].FileHash = "tampered_hash";

        var isValid = VerifyChainLinks(chain);

        // The second record's PreviousHash won't match the tampered first record's FileHash
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyChain_FirstRecordHasNullPreviousHash()
    {
        var chain = BuildChain(2);

        chain[0].PreviousHash.Should().BeNull();
        chain[1].PreviousHash.Should().Be(chain[0].FileHash);
    }

    // =========================================================================
    // ExportChainJson
    // =========================================================================

    [Fact]
    public void ExportChainJson_SerializesCorrectly()
    {
        var chain = BuildChain(2);

        var json = JsonSerializer.Serialize(chain, new JsonSerializerOptions { WriteIndented = true });

        json.Should().Contain("FileHash");
        json.Should().Contain("PreviousHash");
        json.Should().Contain("Signature");
    }

    [Fact]
    public void ExportChainJson_EmptyChain_ReturnsEmptyArray()
    {
        var chain = new List<CustodyRecord>();

        var json = JsonSerializer.Serialize(chain);

        json.Should().Be("[]");
    }

    [Fact]
    public void ExportChainJson_Roundtrips()
    {
        var chain = BuildChain(2);

        var json = JsonSerializer.Serialize(chain);
        var deserialized = JsonSerializer.Deserialize<List<CustodyRecord>>(json);

        deserialized.Should().HaveCount(2);
        deserialized![0].FileHash.Should().Be(chain[0].FileHash);
        deserialized[1].PreviousHash.Should().Be(chain[0].FileHash);
    }

    // =========================================================================
    // CustodyRecord model tests
    // =========================================================================

    [Fact]
    public void CustodyRecord_Defaults()
    {
        var record = new CustodyRecord();

        record.Id.Should().Be(Guid.Empty);
        record.FileHash.Should().BeEmpty();
        record.Signature.Should().BeEmpty();
        record.PreviousHash.Should().BeNull();
    }

    // =========================================================================
    // Helper methods — mirrors ChainOfCustodyService logic
    // =========================================================================

    private static async Task<string> ComputeHashAsync(Stream stream)
    {
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    private static string ComputeSignature(CustodyRecord record, byte[] key)
    {
        var data = $"{record.FileHash}:{record.Timestamp:O}:{record.DeviceId}:{record.UserId}:{record.PreviousHash}";
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }

    private static bool VerifyChainLinks(List<CustodyRecord> chain)
    {
        string? expectedPrevious = null;

        foreach (var record in chain)
        {
            if (record.PreviousHash != expectedPrevious)
                return false;

            expectedPrevious = record.FileHash;
        }

        return true;
    }

    private static List<CustodyRecord> BuildChain(int count)
    {
        var chain = new List<CustodyRecord>();
        string? previousHash = null;

        for (int i = 0; i < count; i++)
        {
            var fileHash = Convert.ToHexStringLower(
                SHA256.HashData(Encoding.UTF8.GetBytes($"file-{i}-{Guid.NewGuid()}")));

            var record = new CustodyRecord
            {
                Id = Guid.NewGuid(),
                FileHash = fileHash,
                FilePath = $"/evidence/file{i}.jpg",
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                DeviceId = "TestDevice_Model_Name",
                UserId = Guid.NewGuid(),
                Latitude = 40.7128 + i * 0.001,
                Longitude = -74.006 + i * 0.001,
                PreviousHash = previousHash,
                Signature = "test-signature"
            };

            chain.Add(record);
            previousHash = fileHash;
        }

        return chain;
    }

    private static CustodyRecord CreateTestRecord()
    {
        return new CustodyRecord
        {
            Id = Guid.NewGuid(),
            FileHash = "abc123def456",
            FilePath = "/evidence/photo.jpg",
            Timestamp = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
            DeviceId = "Samsung_Galaxy_S25",
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PreviousHash = null,
            Signature = ""
        };
    }
}

/// <summary>
/// Mirror of CustodyRecord from TheWatch.Mobile.Services
/// </summary>
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
