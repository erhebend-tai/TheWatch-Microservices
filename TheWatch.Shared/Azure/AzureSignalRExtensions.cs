using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Azure;

/// <summary>
/// Extensions to conditionally upgrade self-hosted SignalR to Azure SignalR Service.
/// Call AFTER AddWatchSignalR() — this method replaces the SignalR transport layer
/// while keeping all existing hubs, broadcasters, and hub mappings intact.
///
/// Usage in Program.cs:
///   builder.Services.AddWatchSignalR();
///   builder.Services.AddAzureSignalRIfConfigured(builder.Configuration);
///   ...
///   app.MapWatchHubs();  // same hub mappings work with both transports
/// </summary>
public static class AzureSignalRExtensions
{
    /// <summary>
    /// If Azure:UseAzureSignalR is true and a connection string is provided,
    /// upgrades the SignalR transport from self-hosted to Azure SignalR Service.
    /// Otherwise, self-hosted SignalR is used (no-op).
    /// </summary>
    public static IServiceCollection AddAzureSignalRIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureServiceOptions.SectionName)
            .Get<AzureServiceOptions>();

        if (options is not { UseAzureSignalR: true })
            return services;

        var connectionString = options.SignalRConnectionString
            ?? configuration.GetConnectionString("AzureSignalR");

        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        // AddAzureSignalR replaces the self-hosted transport with Azure SignalR Service.
        // All existing Hub<T> classes, typed clients, and broadcasters continue to work —
        // the only change is the backplane moves from in-process to Azure.
        services.AddSignalR().AddAzureSignalR(azureOptions =>
        {
            azureOptions.ConnectionString = connectionString;
            azureOptions.ServerStickyMode =
                Microsoft.Azure.SignalR.ServerStickyMode.Required;
        });

        return services;
    }
}
