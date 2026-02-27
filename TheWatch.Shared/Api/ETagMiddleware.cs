using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace TheWatch.Shared.Api;

/// <summary>
/// Item 231: ETag / If-None-Match middleware for conditional GET responses.
/// Computes a weak ETag from the SHA-256 hash of the response body for all GET
/// requests and returns 304 Not Modified when the client's <c>If-None-Match</c>
/// header matches the computed ETag.
/// </summary>
/// <remarks>
/// <para>
/// The middleware buffers the response stream so it can compute the hash before
/// flushing to the client. This is acceptable for JSON API payloads which are
/// typically small. For large binary downloads, consider bypassing this middleware
/// or streaming the ETag from a pre-computed value.
/// </para>
/// <para>
/// Weak ETags (prefixed with <c>W/</c>) are used because the representation may
/// differ in encoding (e.g. compression) while remaining semantically equivalent.
/// </para>
/// </remarks>
public class ETagMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Maximum response body size (in bytes) that will be buffered for ETag computation.
    /// Responses larger than this threshold pass through without an ETag header.
    /// Default: 5 MB.
    /// </summary>
    private const int MaxBufferSize = 5 * 1024 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await next(context);
            return;
        }

        // Capture the original response body stream
        var originalStream = context.Response.Body;

        using var bufferStream = new MemoryStream();
        context.Response.Body = bufferStream;

        await next(context);

        // Only compute ETags for successful (2xx) responses with a body
        if (context.Response.StatusCode is < 200 or >= 300 || bufferStream.Length == 0)
        {
            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(originalStream);
            context.Response.Body = originalStream;
            return;
        }

        // Skip oversized responses to avoid excessive memory usage
        if (bufferStream.Length > MaxBufferSize)
        {
            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(originalStream);
            context.Response.Body = originalStream;
            return;
        }

        // Compute weak ETag from SHA-256 of response body
        bufferStream.Position = 0;
        var hash = await SHA256.HashDataAsync(bufferStream);
        var etagValue = $"W/\"{Convert.ToHexStringLower(hash[..16])}\"";

        context.Response.Headers.ETag = etagValue;

        // Check If-None-Match header
        var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && IsMatch(ifNoneMatch, etagValue))
        {
            // Client already has the current representation
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            context.Response.ContentLength = 0;
            context.Response.Body = originalStream;
            return;
        }

        // Write the buffered response to the original stream
        bufferStream.Position = 0;
        context.Response.ContentLength = bufferStream.Length;
        await bufferStream.CopyToAsync(originalStream);
        context.Response.Body = originalStream;
    }

    /// <summary>
    /// Checks whether the <paramref name="etagValue"/> matches any entry in the
    /// comma-separated <paramref name="ifNoneMatch"/> header value.
    /// Supports the wildcard <c>*</c> match per RFC 7232.
    /// </summary>
    private static bool IsMatch(string ifNoneMatch, string etagValue)
    {
        if (ifNoneMatch.Trim() == "*")
            return true;

        // If-None-Match can contain a comma-separated list of ETags
        foreach (var candidate in ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(candidate, etagValue, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
