using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Core.Scanning;
using MicroGen.Generator;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MicroGen.Cli.Commands;

public sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("Root directory containing OpenAPI spec files")]
        [DefaultValue(".")]
        public string InputDirectory { get; init; } = ".";

        [CommandOption("-o|--output <PATH>")]
        [Description("Output directory for generated solutions")]
        [DefaultValue("./generated")]
        public string OutputDirectory { get; init; } = "./generated";

        [CommandOption("-d|--domain <DOMAIN>")]
        [Description("Generate only for a specific domain")]
        public string? Domain { get; init; }

        [CommandOption("-s|--service <SERVICE>")]
        [Description("Generate only for a specific service")]
        public string? Service { get; init; }

        [CommandOption("--dry-run")]
        [Description("Show what would be generated without writing files")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }

        [CommandOption("-c|--config <PATH>")]
        [Description("Path to microgen.json configuration file")]
        public string? ConfigPath { get; init; }

        [CommandOption("--skip-tests")]
        [Description("Skip test project generation")]
        [DefaultValue(false)]
        public bool SkipTests { get; init; }

        [CommandOption("--skip-deploy")]
        [Description("Skip deployment artifact generation")]
        [DefaultValue(false)]
        public bool SkipDeploy { get; init; }

        [CommandOption("--db-provider <PROVIDER>")]
        [Description("Database provider (SqlServer, PostgreSQL)")]
        [DefaultValue("PostgreSQL")]
        public string DbProvider { get; init; } = "PostgreSQL";

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging")]
        [DefaultValue(false)]
        public bool Verbose { get; init; }

        [CommandOption("-t|--source-type <TYPE>")]
        [Description("Source type to scan: openapi, sql, csv, graphql, html, or all (default)")]
        [DefaultValue("all")]
        public string SourceType { get; init; } = "all";
    }

    public override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] Settings settings)
    {
        var inputPath = Path.GetFullPath(settings.InputDirectory);
        var outputPath = Path.GetFullPath(settings.OutputDirectory);

        if (!Directory.Exists(inputPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Input directory not found: [yellow]{inputPath}[/]");
            return 1;
        }

        // Load or build config
        var config = LoadConfig(settings);

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(settings.Verbose ? LogLevel.Debug : LogLevel.Information);
            builder.AddConsole();
        });
        var logger = loggerFactory.CreateLogger<SolutionGenerator>();

        // Banner
        AnsiConsole.Write(new FigletText("MicroGen").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[dim]Input:  {inputPath}[/]");
        AnsiConsole.MarkupLine($"[dim]Output: {outputPath}[/]");
        if (settings.DryRun)
            AnsiConsole.MarkupLine("[yellow]DRY RUN — no files will be written[/]");
        AnsiConsole.WriteLine();

        // Scan
        var domains = await AnsiConsole.Status()
            .StartAsync("Scanning source specs...", async ctx =>
            {
                var includePatterns = ParserFactory.GetIncludePatterns(settings.SourceType);
                var scanConfig = new GeneratorConfig
                {
                    IncludePatterns = includePatterns
                };

                if (settings.SourceType.Equals("openapi", StringComparison.OrdinalIgnoreCase))
                {
                    var parser = new SpecParser(loggerFactory.CreateLogger<SpecParser>());
                    var scanner = new SpecScanner(loggerFactory.CreateLogger<SpecScanner>(), parser);
                    return await scanner.ScanAsync(inputPath, scanConfig);
                }
                else
                {
                    var parsers = ParserFactory.BuildParserList(settings.SourceType, loggerFactory);
                    var scanner = new SourceScanner(loggerFactory.CreateLogger<SourceScanner>(), parsers);
                    return await scanner.ScanAsync(inputPath, scanConfig);
                }
            });

        // Filter
        if (!string.IsNullOrWhiteSpace(settings.Domain))
        {
            domains = domains
                .Where(d => d.Name.Equals(settings.Domain, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(settings.Service))
        {
            foreach (var d in domains)
            {
                d.Services.RemoveAll(s =>
                    !s.PascalName.Equals(settings.Service, StringComparison.OrdinalIgnoreCase) &&
                    !s.SpecFilePath.Contains(settings.Service, StringComparison.OrdinalIgnoreCase));
            }
            domains = domains.Where(d => d.Services.Count > 0).ToList();
        }

        var totalServices = domains.Sum(d => d.Services.Count);
        if (totalServices == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No matching services found.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"Found [green]{totalServices}[/] services across [cyan]{domains.Count}[/] domains\n");

        // Generate
        var generator = new SolutionGenerator(logger, config);

        GenerationSummary summary = null!;
        await AnsiConsole.Progress()
            .AutoRefresh(true)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Generating...", maxValue: totalServices);

                var results = new List<GenerationResult>();
                foreach (var domain in domains)
                {
                    foreach (var service in domain.Services)
                    {
                        task.Description = $"Generating {service.PascalName}...";
                        var result = await generator.GenerateServiceAsync(service);
                        results.Add(result);
                        task.Increment(1);
                    }

                    // Domain-level gateway (Nginx + YARP API Gateway)
                    task.Description = $"Generating {domain.Name} gateway...";
                    await generator.GenerateDomainGatewayAsync(domain);
                }

                summary = new GenerationSummary
                {
                    Results = results,
                    TotalDuration = TimeSpan.FromMilliseconds(
                        results.Sum(r => r.Duration.TotalMilliseconds))
                };
            });

        // Report
        AnsiConsole.WriteLine();
        RenderReport(summary);

        return summary.Results.Any(r => !r.Success) ? 1 : 0;
    }

    private static GeneratorConfig LoadConfig(Settings settings)
    {
        var config = new GeneratorConfig
        {
            OutputDirectory = Path.GetFullPath(settings.OutputDirectory),
            DryRun = settings.DryRun,
            SkipTests = settings.SkipTests,
            Database = new DatabaseConfig { Provider = settings.DbProvider }
        };

        // Try loading from file
        if (!string.IsNullOrWhiteSpace(settings.ConfigPath) && File.Exists(settings.ConfigPath))
        {
            // TODO: Deserialize config from JSON file
        }

        return config;
    }

    private static void RenderReport(GenerationSummary summary)
    {
        var table = new Table()
            .Title("[bold]Generation Report[/]")
            .AddColumn("Service")
            .AddColumn("Domain")
            .AddColumn("Files")
            .AddColumn("Duration")
            .AddColumn("Status");

        foreach (var result in summary.Results.OrderBy(r => r.DomainName).ThenBy(r => r.ServiceName))
        {
            var status = result.Success
                ? "[green]OK[/]"
                : $"[red]FAILED[/] ({result.Errors.Count} errors)";

                table.AddRow(
                    result.ServiceName,
                    result.DomainName,
                    result.GeneratedFiles.Count.ToString(CultureInfo.InvariantCulture),
                    $"{result.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms",
                    status);
        }

        AnsiConsole.Write(table);

        // Totals
        var totalFiles = summary.Results.Sum(r => r.GeneratedFiles.Count);
        var failed = summary.Results.Count(r => !r.Success);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total:[/] {summary.Results.Count} services, {totalFiles} files in {summary.TotalDuration.TotalSeconds:F1}s");

        if (failed > 0)
            AnsiConsole.MarkupLine($"[red]{failed} service(s) failed[/]");
        else
            AnsiConsole.MarkupLine("[green]All services generated successfully![/]");
    }
}
