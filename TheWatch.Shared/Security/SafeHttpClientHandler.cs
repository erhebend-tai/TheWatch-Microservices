using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Security;

/// <summary>
/// SSRF (Server-Side Request Forgery) protection handler for <see cref="HttpClient"/>.
/// Resolves DNS before sending and blocks requests to private, loopback, and link-local
/// IP ranges. Use as the inner handler when constructing <see cref="HttpClient"/> instances
/// for user-influenced or external URLs.
/// </summary>
/// <remarks>
/// Blocked ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 127.0.0.0/8,
/// 169.254.0.0/16 (AWS metadata / link-local), ::1, fd00::/8.
/// </remarks>
public class SafeHttpClientHandler : HttpClientHandler
{
    private readonly ILogger<SafeHttpClientHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SafeHttpClientHandler"/>.
    /// </summary>
    /// <param name="logger">Logger for recording blocked SSRF attempts.</param>
    public SafeHttpClientHandler(ILogger<SafeHttpClientHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            throw new InvalidOperationException("Request URI must not be null.");

        var host = request.RequestUri.Host;

        // Resolve DNS to get actual IP addresses before sending
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex,
                "[SEC:SSRF_DNS_FAIL] DNS resolution failed for {Host}", host);
            throw new InvalidOperationException(
                "Request to internal network address is blocked (SSRF protection)");
        }

        foreach (var address in addresses)
        {
            if (IsPrivateOrReserved(address))
            {
                _logger.LogWarning(
                    "[SEC:SSRF_BLOCKED] Blocked request to {Host} resolving to private address {Address}",
                    host, address);
                throw new InvalidOperationException(
                    "Request to internal network address is blocked (SSRF protection)");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Determines whether an IP address belongs to a private, loopback, link-local,
    /// or otherwise reserved range that should not be reachable from external requests.
    /// </summary>
    private static bool IsPrivateOrReserved(IPAddress address)
    {
        // Handle IPv4-mapped IPv6 addresses (e.g., ::ffff:10.0.0.1)
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        // IPv6 checks
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // ::1 loopback
            if (IPAddress.IPv6Loopback.Equals(address))
                return true;

            // fd00::/8 — unique local addresses
            var bytes = address.GetAddressBytes();
            if (bytes.Length >= 1 && (bytes[0] & 0xFF) == 0xFD)
                return true;

            // fe80::/10 — link-local
            if (bytes.Length >= 2 && bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                return true;

            return false;
        }

        // IPv4 checks
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes.Length < 4) return false;

            // 127.0.0.0/8 — loopback
            if (bytes[0] == 127)
                return true;

            // 10.0.0.0/8 — private class A
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12 — private class B
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16 — private class C
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 — link-local / AWS metadata endpoint
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 0.0.0.0/8 — "this" network
            if (bytes[0] == 0)
                return true;

            return false;
        }

        // Unknown address family — block by default
        return true;
    }
}
