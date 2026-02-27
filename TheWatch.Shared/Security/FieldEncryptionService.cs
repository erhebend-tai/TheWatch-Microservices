using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace TheWatch.Shared.Security;

/// <summary>
/// Application-level field encryption interface for CUI database columns.
/// Encrypts individual field values so they remain protected even if the
/// database or backups are compromised (defense in depth beyond TDE).
/// </summary>
public interface IFieldEncryptor
{
    /// <summary>
    /// Encrypts a plaintext value using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The value to encrypt.</param>
    /// <returns>A Base64-encoded string containing nonce + ciphertext + authentication tag.</returns>
    Task<string> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts a value previously encrypted by <see cref="EncryptAsync"/>.
    /// </summary>
    /// <param name="ciphertext">The Base64-encoded encrypted value.</param>
    /// <returns>The original plaintext value.</returns>
    /// <exception cref="CryptographicException">Thrown if the ciphertext has been tampered with.</exception>
    Task<string> DecryptAsync(string ciphertext);
}

/// <summary>
/// AES-256-GCM field encryption service for CUI database columns. Provides
/// authenticated encryption with a unique nonce per operation. Output format is
/// <c>Base64(nonce[12] + ciphertext[N] + tag[16])</c> for single-column storage.
/// </summary>
/// <remarks>
/// Thread-safe: each encryption generates a fresh 12-byte nonce via
/// <see cref="RandomNumberGenerator"/>. The 256-bit key is loaded from
/// <c>Security:FieldEncryptionKey</c> configuration (Base64-encoded, 32 bytes).
/// </remarks>
public class AesGcmFieldEncryptor : IFieldEncryptor, IDisposable
{
    private const int NonceSize = 12;   // 96 bits — GCM standard
    private const int TagSize = 16;     // 128 bits — GCM standard
    private const int KeySize = 32;     // 256 bits — AES-256

    private readonly byte[] _key;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="AesGcmFieldEncryptor"/>.
    /// </summary>
    /// <param name="configuration">
    /// Application configuration. Reads <c>Security:FieldEncryptionKey</c> as a
    /// Base64-encoded 32-byte key.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the key is not configured or is not exactly 32 bytes.
    /// </exception>
    public AesGcmFieldEncryptor(IConfiguration configuration)
    {
        var keyBase64 = configuration["Security:FieldEncryptionKey"]
            ?? throw new InvalidOperationException(
                "FATAL: Security:FieldEncryptionKey is not configured. " +
                "Provide a Base64-encoded 256-bit (32-byte) key.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != KeySize)
        {
            throw new InvalidOperationException(
                $"Security:FieldEncryptionKey must be exactly {KeySize} bytes (256 bits). " +
                $"Received {_key.Length} bytes.");
        }
    }

    /// <inheritdoc />
    public Task<string> EncryptAsync(string plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Output format: nonce[12] + ciphertext[N] + tag[16]
        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceSize);
        tag.CopyTo(result, NonceSize + ciphertext.Length);

        return Task.FromResult(Convert.ToBase64String(result));
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string ciphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(ciphertext);

        var encryptedBytes = Convert.FromBase64String(ciphertext);

        if (encryptedBytes.Length < NonceSize + TagSize)
        {
            throw new CryptographicException(
                "Encrypted data is too short to contain a valid nonce and authentication tag.");
        }

        var nonce = encryptedBytes.AsSpan(0, NonceSize);
        var ciphertextLength = encryptedBytes.Length - NonceSize - TagSize;
        var ciphertextSpan = encryptedBytes.AsSpan(NonceSize, ciphertextLength);
        var tag = encryptedBytes.AsSpan(NonceSize + ciphertextLength, TagSize);

        var plaintextBytes = new byte[ciphertextLength];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, ciphertextSpan, tag, plaintextBytes);

        return Task.FromResult(System.Text.Encoding.UTF8.GetString(plaintextBytes));
    }

    /// <summary>
    /// Securely zeros the key material on disposal.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_key);
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
