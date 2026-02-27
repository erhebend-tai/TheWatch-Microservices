using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MicroGen.Core.Configuration;
using MicroGen.Core.Scanning;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MicroGen.Cli.Commands;

public sealed class ScanCommand : AsyncCommand<ScanCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("Root directory containing OpenAPI spec files")]
        [DefaultValue(".")]
        public string InputDirectory { get; init; } = ".";

        [CommandOption("-d|--domain <DOMAIN>")]
        [Description("Filter to a specific domain (e.g., emergency, dispatch)")]
        public string? Domain { get; init; }

        [CommandOption("--exclude <PATTERN>")]
        [Description("Comma-separated glob patterns to exclude")]
        public string? ExcludePatterns { get; init; }

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

        if (!Directory.Exists(inputPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: [yellow]{inputPath}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[blue]Scanning[/] {inputPath}...\n");

        var exclusions = settings.ExcludePatterns?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray() ?? Array.Empty<string>();

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Warning));

        var includePatterns = ParserFactory.GetIncludePatterns(settings.SourceType);
        var config = new GeneratorConfig
        {
            IncludePatterns = includePatterns,
            ExcludePatterns = exclusions,
            Recursive = true
        };

        List<MicroGen.Core.Models.DomainDescriptor> domains;

        if (settings.SourceType.Equals("openapi", StringComparison.OrdinalIgnoreCase))
        {
            // Legacy path: use SpecScanner for pure OpenAPI
            var parser = new SpecParser(loggerFactory.CreateLogger<SpecParser>());
            var scanner = new SpecScanner(loggerFactory.CreateLogger<SpecScanner>(), parser);
            domains = await scanner.ScanAsync(inputPath, config);
        }
        else
        {
            // Multi-format: use SourceScanner
            var parsers = ParserFactory.BuildParserList(settings.SourceType, loggerFactory);
            var scanner = new SourceScanner(loggerFactory.CreateLogger<SourceScanner>(), parsers);
            domains = await scanner.ScanAsync(inputPath, config);
        }

        // Filter by domain if specified
        if (!string.IsNullOrWhiteSpace(settings.Domain))
        {
            domains = domains
                .Where(d => d.Name.Equals(settings.Domain, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (domains.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No specs found for domain:[/] {settings.Domain}");
                return 0;
            }
        }

        // Summary tree
        var tree = new Tree("[bold]Discovered Source Specs[/]");

        var totalSpecs = 0;
        var totalOps = 0;
        var totalSchemas = 0;

        foreach (var domain in domains.OrderBy(d => d.Name))
        {
            var domainNode = tree.AddNode($"[cyan]{domain.Name}[/] ({domain.Services.Count} specs)");

            foreach (var service in domain.Services.OrderBy(s => s.PascalName))
            {
                totalSpecs++;
                totalOps += service.Operations.Count;
                totalSchemas += service.Schemas.Count;

                var svcNode = domainNode.AddNode(
                    $"[green]{service.PascalName}[/] v{service.Version}  " +
                    $"[dim]({service.Operations.Count} ops, {service.Schemas.Count} schemas)[/]");

                // Tags
                foreach (var tag in service.Tags)
                {
                    var tagOps = service.Operations.Count(o =>
                        o.Tag.Equals(tag.Name, StringComparison.OrdinalIgnoreCase));
                    svcNode.AddNode($"[yellow]{tag.PascalName}Controller[/] ({tagOps} endpoints)");
                }
            }
        }

        AnsiConsole.Write(tree);

        // Summary table
        AnsiConsole.WriteLine();
        var table = new Table()
            .AddColumn("Metric")
            .AddColumn("Count");

        table.AddRow("Domains", domains.Count.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Specs", totalSpecs.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Operations", totalOps.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Schemas", totalSchemas.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Controllers", domains.Sum(d =>
            d.Services.Sum(s => s.Tags.Count)).ToString(CultureInfo.InvariantCulture));

        AnsiConsole.Write(table);

        return 0;
    }
}
