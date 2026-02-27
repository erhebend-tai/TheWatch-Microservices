using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MicroGen.Core.Scanning;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MicroGen.Cli.Commands;

public sealed class ParseCommand : AsyncCommand<ParseCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--file <PATH>")]
        [Description("Path to a source file (SQL, CSV, GraphQL, OpenAPI, HTML)")]
        public string? FilePath { get; init; }

        [CommandOption("-u|--url <URL>")]
        [Description("URL to parse (Swagger UI, web page)")]
        public string? Url { get; init; }

        [CommandOption("-t|--source-type <TYPE>")]
        [Description("Force a specific parser (openapi, sql, csv, graphql, html)")]
        public string? SourceType { get; init; }

        [CommandOption("-d|--domain <NAME>")]
        [Description("Domain name for the parsed service")]
        [DefaultValue("Default")]
        public string Domain { get; init; } = "Default";

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(FilePath) && string.IsNullOrWhiteSpace(Url))
                return ValidationResult.Error("Either --file or --url must be specified.");
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(Url))
                return ValidationResult.Error("Specify either --file or --url, not both.");
            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] Settings settings)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Debug));

        var source = settings.FilePath ?? settings.Url!;
        var isUrl = !string.IsNullOrWhiteSpace(settings.Url);

        if (!isUrl && !File.Exists(source))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{source}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[blue]Parsing[/] {source}...\n");

        var parsers = ParserFactory.BuildParserList(settings.SourceType, loggerFactory);
        ISourceParser? parser;

        if (!string.IsNullOrWhiteSpace(settings.SourceType))
        {
            parser = parsers.FirstOrDefault(p => p.CanParse(source));
        }
        else
        {
            var ext = isUrl ? ".html" : Path.GetExtension(source);
            parser = parsers.FirstOrDefault(p =>
                p.SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                ?? parsers.FirstOrDefault(p => p.CanParse(source));
        }

        if (parser is null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No parser found for [yellow]{source}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[dim]Using parser:[/] {parser.GetType().Name}");
        AnsiConsole.WriteLine();

        try
        {
            var service = await parser.ParseAsync(source, settings.Domain);
            RenderServiceDescriptor(service);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Parse error:[/] {ex.Message}");
            return 1;
        }
    }

    private static void RenderServiceDescriptor(Core.Models.ServiceDescriptor service)
    {
        var tree = new Tree($"[bold cyan]{service.PascalName}[/] [dim]({service.DomainName})[/]");

        // Metadata
        var metaNode = tree.AddNode("[bold]Metadata[/]");
        metaNode.AddNode($"Title: [green]{Markup.Escape(service.Title)}[/]");
        if (!string.IsNullOrWhiteSpace(service.Description))
            metaNode.AddNode($"Description: [dim]{Markup.Escape(Truncate(service.Description, 100))}[/]");
        metaNode.AddNode($"Version: {service.Version}");
        metaNode.AddNode($"Source: [dim]{Markup.Escape(service.SpecFilePath)}[/]");
        if (service.SecuritySchemes.Count > 0)
            metaNode.AddNode($"Security: {string.Join(", ", service.SecuritySchemes)}");

        // Tags
        if (service.Tags.Count > 0)
        {
            var tagsNode = tree.AddNode($"[bold]Tags[/] ({service.Tags.Count})");
            foreach (var tag in service.Tags.Take(20))
            {
                var opCount = service.Operations.Count(o =>
                    o.Tag.Equals(tag.Name, StringComparison.OrdinalIgnoreCase));
                tagsNode.AddNode($"[yellow]{Markup.Escape(tag.PascalName)}[/] ({opCount} ops)");
            }
            if (service.Tags.Count > 20)
                tagsNode.AddNode($"[dim]... and {service.Tags.Count - 20} more[/]");
        }

        // Operations
        if (service.Operations.Count > 0)
        {
            var opsNode = tree.AddNode($"[bold]Operations[/] ({service.Operations.Count})");
            foreach (var op in service.Operations.Take(30))
            {
                var methodColor = op.HttpMethod switch
                {
                    "GET" => "green",
                    "POST" => "blue",
                    "PUT" => "yellow",
                    "DELETE" => "red",
                    "PATCH" => "magenta",
                    _ => "white"
                };
                var opNode = opsNode.AddNode(
                    $"[{methodColor}]{op.HttpMethod}[/] {Markup.Escape(op.Path)} [dim]({Markup.Escape(op.OperationId)})[/]");

                if (op.PathParameters.Count > 0)
                    opNode.AddNode($"Path params: {string.Join(", ", op.PathParameters.Select(p => $"{p.Name}:{p.Type}"))}");
                if (op.QueryParameters.Count > 0)
                    opNode.AddNode($"Query params: {string.Join(", ", op.QueryParameters.Select(p => $"{p.Name}:{p.Type}"))}");
                if (op.HasRequestBody)
                    opNode.AddNode($"Body: {op.RequestBody!.Name}");
            }
            if (service.Operations.Count > 30)
                opsNode.AddNode($"[dim]... and {service.Operations.Count - 30} more[/]");
        }

        // Schemas
        if (service.Schemas.Count > 0)
        {
            var schemasNode = tree.AddNode($"[bold]Schemas[/] ({service.Schemas.Count})");
            foreach (var schema in service.Schemas.Take(30))
            {
                var kind = schema.IsEnum ? "[magenta]enum[/]" :
                           schema.IsArray ? "[blue]array[/]" : "[green]object[/]";
                var schemaNode = schemasNode.AddNode($"{kind} [bold]{Markup.Escape(schema.Name)}[/]");

                if (schema.IsEnum)
                {
                    schemaNode.AddNode($"Values: {string.Join(", ", schema.EnumValues.Take(10))}");
                }
                else
                {
                    foreach (var prop in schema.Properties.Take(15))
                    {
                        var req = prop.Required ? "[red]*[/]" : "";
                        var typeStr = prop.RefSchemaName ?? $"{prop.Type}" + (prop.Format is not null ? $"/{prop.Format}" : "");
                        schemaNode.AddNode($"{Markup.Escape(prop.Name)}{req}: [dim]{Markup.Escape(typeStr)}[/]");
                    }
                    if (schema.Properties.Count > 15)
                        schemaNode.AddNode($"[dim]... and {schema.Properties.Count - 15} more properties[/]");
                }
            }
            if (service.Schemas.Count > 30)
                schemasNode.AddNode($"[dim]... and {service.Schemas.Count - 30} more[/]");
        }

        AnsiConsole.Write(tree);

        // Summary table
        AnsiConsole.WriteLine();
        var table = new Table()
            .AddColumn("Metric")
            .AddColumn("Count");
        table.AddRow("Tags", service.Tags.Count.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Operations", service.Operations.Count.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Schemas", service.Schemas.Count.ToString(CultureInfo.InvariantCulture));
        table.AddRow("  - Entities", service.Schemas.Count(s => s.IsEntity).ToString(CultureInfo.InvariantCulture));
        table.AddRow("  - Enums", service.Schemas.Count(s => s.IsEnum).ToString(CultureInfo.InvariantCulture));
        table.AddRow("GET", service.Operations.Count(o => o.IsQuery).ToString(CultureInfo.InvariantCulture));
        table.AddRow("POST/PUT/PATCH/DELETE", service.Operations.Count(o => o.IsCommand).ToString(CultureInfo.InvariantCulture));
        AnsiConsole.Write(table);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
