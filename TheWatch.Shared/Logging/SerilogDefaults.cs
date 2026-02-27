using Serilog;
using Serilog.Events;
using TheWatch.Shared.Security;

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

    /// <summary>Number of daily log files to retain for general services (NIST AU-11: 90 days).</summary>
    public const int RetainedFileCount = 90;

    /// <summary>Number of daily log files to retain for security/audit services (NIST AU-11: 365 days).</summary>
    public const int SecurityRetainedFileCount = 365;

    /// <summary>
    /// Creates the standard TheWatch Serilog configuration.
    /// Prefer the generated <c>SerilogSetup.ConfigureWatchSerilog()</c> extension method instead.
    /// </summary>
    /// <param name="serviceName">The service name embedded as a log property.</param>
    /// <param name="isSecurity">
    /// Set to <c>true</c> for security/audit services (e.g. P5.AuthSecurity) to apply the
    /// extended <see cref="SecurityRetainedFileCount"/> (365-day) log retention policy required
    /// by NIST AU-11. Defaults to <c>false</c> (90-day general retention).
    /// </param>
    public static LoggerConfiguration CreateDefaultConfiguration(string serviceName, bool isSecurity = false)
    {
        var retainedFileCount = isSecurity ? SecurityRetainedFileCount : RetainedFileCount;
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
            // Item 364: mask PII (emails, phones, IPs, SSNs, GPS coords) in all log properties
            .Enrich.With<PiiMaskingEnricher>()
            .WriteTo.Console(outputTemplate: ConsoleTemplate)
            .WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCount,
                outputTemplate: FileTemplate);
    }

    /// <summary>
    /// Flush and close the logger on shutdown.
    /// </summary>
    public static void CloseAndFlush() => Log.CloseAndFlush();
}
