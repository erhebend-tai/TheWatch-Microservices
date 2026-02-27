using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// Master extension for registering all Cloudflare edge service providers.
/// Each provider is independently toggled via Cloudflare:{toggle} in appsettings.json.
///
/// Usage in Program.cs:
///   builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);
///
/// When no toggle is enabled, all NoOp implementations are registered (safe for dev).
/// When a toggle is enabled + credentials configured, the real Cloudflare stub is registered
/// (throws NotImplementedException until batch-implemented).
/// </summary>
public static class CloudflareServiceExtensions
{
    public static IServiceCollection AddCloudflareServicesIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(CloudflareOptions.SectionName)
            .Get<CloudflareOptions>() ?? new CloudflareOptions();

        services.AddSingleton(options);

        // ─── Item 136: CDN ───
        if (options.UseCdn &&
            !string.IsNullOrWhiteSpace(options.ApiToken) &&
            !string.IsNullOrWhiteSpace(options.ZoneId))
        {
            services.AddHttpClient<CloudflareCdnProvider>();
            services.AddSingleton<ICdnProvider>(sp =>
                new CloudflareCdnProvider(
                    options,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CloudflareCdnProvider)),
                    sp.GetRequiredService<ILogger<CloudflareCdnProvider>>()));
        }
        else
        {
            services.AddSingleton<ICdnProvider>(sp =>
                new NoOpCdnProvider(
                    sp.GetRequiredService<ILogger<NoOpCdnProvider>>()));
        }

        // ─── Item 137: Workers Edge Auth ───
        if (options.UseWorkersAuth &&
            !string.IsNullOrWhiteSpace(options.ZeroTrustTeamDomain))
        {
            services.AddHttpClient<CloudflareWorkersAuthProvider>();
            services.AddSingleton<IEdgeAuthProvider>(sp =>
                new CloudflareWorkersAuthProvider(
                    options,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CloudflareWorkersAuthProvider)),
                    sp.GetRequiredService<ILogger<CloudflareWorkersAuthProvider>>()));
        }
        else
        {
            services.AddSingleton<IEdgeAuthProvider>(sp =>
                new NoOpEdgeAuthProvider(
                    sp.GetRequiredService<ILogger<NoOpEdgeAuthProvider>>()));
        }

        // ─── Item 138: WAF ───
        if (options.UseWaf &&
            !string.IsNullOrWhiteSpace(options.ApiToken) &&
            !string.IsNullOrWhiteSpace(options.ZoneId))
        {
            services.AddHttpClient<CloudflareWafProvider>();
            services.AddSingleton<IWafProvider>(sp =>
                new CloudflareWafProvider(
                    options,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CloudflareWafProvider)),
                    sp.GetRequiredService<ILogger<CloudflareWafProvider>>()));
        }
        else
        {
            services.AddSingleton<IWafProvider>(sp =>
                new NoOpWafProvider(
                    sp.GetRequiredService<ILogger<NoOpWafProvider>>()));
        }

        // ─── Items 139-140: Zero Trust + Argo Tunnels ───
        if ((options.UseZeroTrust || options.UseArgoTunnels) &&
            !string.IsNullOrWhiteSpace(options.ApiToken) &&
            !string.IsNullOrWhiteSpace(options.AccountId))
        {
            services.AddHttpClient<CloudflareTunnelProvider>();
            services.AddSingleton<ITunnelProvider>(sp =>
                new CloudflareTunnelProvider(
                    options,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CloudflareTunnelProvider)),
                    sp.GetRequiredService<ILogger<CloudflareTunnelProvider>>()));
        }
        else
        {
            services.AddSingleton<ITunnelProvider>(sp =>
                new NoOpTunnelProvider(
                    sp.GetRequiredService<ILogger<NoOpTunnelProvider>>()));
        }

        return services;
    }
}
