using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using MicroGen.Core.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MicroGen.Cli.Commands;

public sealed class InitCommand : AsyncCommand<InitCommand.Settings>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-o|--output <PATH>")]
        [Description("Directory to create the configuration file in")]
        [DefaultValue(".")]
        public string OutputDirectory { get; init; } = ".";
    }

    public override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] Settings settings)
    {
        var outputPath = Path.GetFullPath(settings.OutputDirectory);
        var configPath = Path.Combine(outputPath, "microgen.json");

        if (File.Exists(configPath))
        {
            var overwrite = AnsiConsole.Confirm(
                $"[yellow]microgen.json already exists at {outputPath}. Overwrite?[/]",
                defaultValue: false);

            if (!overwrite)
            {
                AnsiConsole.MarkupLine("[dim]Aborted.[/]");
                return 0;
            }
        }

        var config = new GeneratorConfig
        {
            OutputDirectory = "./generated",
            TargetFramework = "net10.0",
            OutputStructure = "domain-grouped",
            DryRun = false,
            SkipTests = false,
            Features = new FeatureConfig
            {
                Hangfire = true,
                BlazorDashboard = true,
                Redis = true,
                OpenTelemetry = true,
                SignalR = true,
                Polly = true,
                Serilog = true,
                VoicePipeline = true
            },
            Database = new DatabaseConfig
            {
                Provider = "PostgreSQL",
                MigrationsEnabled = true,
                SeedDataEnabled = true
            },
            Logging = new LoggingConfig
            {
                Sinks = ["Console", "File"],
                MinimumLevel = "Information"
            },
            Telemetry = new TelemetryConfig
            {
                Exporters = ["Console", "Prometheus", "OTLP"]
            },
            Caching = new CachingConfig
            {
                DefaultTtlSeconds = 300,
                Provider = "Redis"
            }
        };

        Directory.CreateDirectory(outputPath);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(configPath, json);

        AnsiConsole.MarkupLine($"[green]Created[/] {configPath}");
        AnsiConsole.MarkupLine("[dim]Edit the file to customize generation settings.[/]");

        return 0;
    }
}
