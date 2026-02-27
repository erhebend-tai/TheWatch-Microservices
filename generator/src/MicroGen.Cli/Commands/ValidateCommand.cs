using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MicroGen.Core.Configuration;
using MicroGen.Core.Scanning;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MicroGen.Cli.Commands;

public sealed class ValidateCommand : AsyncCommand<ValidateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("Root directory containing OpenAPI spec files")]
        [DefaultValue(".")]
        public string InputDirectory { get; init; } = ".";

        [CommandOption("-d|--domain <DOMAIN>")]
        [Description("Validate only a specific domain")]
        public string? Domain { get; init; }

        [CommandOption("--strict")]
        [Description("Treat warnings as errors")]
        [DefaultValue(false)]
        public bool Strict { get; init; }
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

        AnsiConsole.MarkupLine($"[blue]Validating[/] specs in {inputPath}...\n");

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Warning));
        var parser = new SpecParser(loggerFactory.CreateLogger<SpecParser>());
        var scanner = new SpecScanner(loggerFactory.CreateLogger<SpecScanner>(), parser);

        var config = new GeneratorConfig();

        var domains = await scanner.ScanAsync(inputPath, config);

        if (!string.IsNullOrWhiteSpace(settings.Domain))
        {
            domains = domains
                .Where(d => d.Name.Equals(settings.Domain, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalSpecs = 0;
        var totalErrors = 0;
        var totalWarnings = 0;

        var table = new Table()
            .Title("[bold]Validation Results[/]")
            .AddColumn("Spec")
            .AddColumn("Domain")
            .AddColumn("Ops")
            .AddColumn("Schemas")
            .AddColumn("Issues")
            .AddColumn("Status");

        foreach (var domain in domains.OrderBy(d => d.Name))
        {
            foreach (var service in domain.Services.OrderBy(s => s.PascalName))
            {
                totalSpecs++;
                var issues = new List<string>();

                // Validate operationIds
                var opsWithoutId = service.Operations.Where(o =>
                    string.IsNullOrWhiteSpace(o.OperationId)).ToList();
                if (opsWithoutId.Count > 0)
                {
                    issues.Add($"{opsWithoutId.Count} ops missing operationId");
                    totalErrors += opsWithoutId.Count;
                }

                // Validate tags
                var opsWithoutTag = service.Operations.Where(o =>
                    string.IsNullOrWhiteSpace(o.Tag)).ToList();
                if (opsWithoutTag.Count > 0)
                {
                    issues.Add($"{opsWithoutTag.Count} ops missing tags");
                    totalWarnings += opsWithoutTag.Count;
                }

                // Validate schemas have descriptions
                var schemasWithoutDesc = service.Schemas.Where(s =>
                    string.IsNullOrWhiteSpace(s.Description)).ToList();
                if (schemasWithoutDesc.Count > 0)
                {
                    issues.Add($"{schemasWithoutDesc.Count} schemas missing descriptions");
                    totalWarnings += schemasWithoutDesc.Count;
                }

                // Validate operations have summaries
                var opsWithoutSummary = service.Operations.Where(o =>
                    string.IsNullOrWhiteSpace(o.Summary)).ToList();
                if (opsWithoutSummary.Count > 0)
                {
                    issues.Add($"{opsWithoutSummary.Count} ops missing summaries");
                    totalWarnings += opsWithoutSummary.Count;
                }

                var status = issues.Count == 0
                    ? "[green]PASS[/]"
                    : issues.Any(i => i.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                      i.Contains("missing operationId"))
                        ? "[red]FAIL[/]"
                        : "[yellow]WARN[/]";

                table.AddRow(
                    service.PascalName,
                    domain.Name,
                    service.Operations.Count.ToString(CultureInfo.InvariantCulture),
                    service.Schemas.Count.ToString(CultureInfo.InvariantCulture),
                    issues.Count > 0 ? string.Join("; ", issues) : "[dim]none[/]",
                    status);
            }
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Summary:[/] {totalSpecs} specs, {totalErrors} errors, {totalWarnings} warnings");

        if (totalErrors > 0)
        {
            AnsiConsole.MarkupLine("[red]Validation failed![/]");
            return 1;
        }

        if (totalWarnings > 0 && settings.Strict)
        {
            AnsiConsole.MarkupLine("[red]Validation failed (strict mode — warnings are errors)![/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Validation passed![/]");
        return 0;
    }
}
