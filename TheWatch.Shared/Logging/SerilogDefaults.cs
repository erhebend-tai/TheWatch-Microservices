using Serilog;
using Serilog.Events;

namespace TheWatch.Shared.Logging;

/// <summary>
/// Shared Serilog configuration constants and helpers.
/// The actual wiring is done by the auto-generated SerilogSetup class per service.
/// </summary>
public static class SerilogDefaults
{
    public const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}";

    public const string FileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Service}] {SourceContext} {Message:lj}{NewLine}{Exception}";

    public const int RetainedFileCount = 30;

    /// <summary>
    /// Creates the standard TheWatch Serilog configuration.
    /// Prefer the generated <c>SerilogSetup.ConfigureWatchSerilog()</c> extension method instead.
    /// </summary>
    public static LoggerConfiguration CreateDefaultConfiguration(string serviceName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate: ConsoleTemplate)
            .WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedFileCount,
                outputTemplate: FileTemplate);
    }

    /// <summary>
    /// Flush and close the logger on shutdown.
    /// </summary>
    public static void CloseAndFlush() => Log.CloseAndFlush();
}
