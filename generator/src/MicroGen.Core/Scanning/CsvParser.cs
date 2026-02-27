using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Parses CSV files into <see cref="ServiceDescriptor"/> objects.
/// Supports two modes:
/// 1. Function catalog (columns include function_name) — each row becomes an operation.
/// 2. Generic data — headers become schema properties with auto-generated CRUD operations.
/// </summary>
public sealed class CsvParser : ISourceParser
{
    private readonly ILogger<CsvParser> _logger;

    // Column names that trigger function catalog mode
    private static readonly HashSet<string> FunctionCatalogColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "function_name", "functionname", "function", "method_name", "methodname"
    };

    private static readonly HashSet<string> CategoryColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "category", "group", "module", "tag"
    };

    private static readonly HashSet<string> SubcategoryColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "subcategory", "subgroup", "submodule", "subtag"
    };

    public CsvParser(ILogger<CsvParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => [".csv"];

    public bool CanParse(string pathOrUri) =>
        Path.GetExtension(pathOrUri).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<ServiceDescriptor> ParseAsync(string pathOrUri, string domainName, CancellationToken ct = default)
    {
        _logger.LogDebug("Parsing CSV file {File}", pathOrUri);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(pathOrUri);
        using var csv = new CsvReader(reader, csvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        // Detect mode
        var functionNameCol = headers.FirstOrDefault(h => FunctionCatalogColumns.Contains(h));
        var categoryCol = headers.FirstOrDefault(h => CategoryColumns.Contains(h));
        var subcategoryCol = headers.FirstOrDefault(h => SubcategoryColumns.Contains(h));

        var isFunctionCatalog = functionNameCol is not null;

        var serviceName = NamingHelper.ServiceNameFromFile(pathOrUri);

        ServiceDescriptor descriptor;
        if (isFunctionCatalog)
        {
            descriptor = await ParseFunctionCatalogAsync(csv, headers, functionNameCol!, categoryCol, subcategoryCol,
                serviceName, domainName, pathOrUri, ct);
        }
        else
        {
            descriptor = await ParseGenericDataAsync(csv, headers, serviceName, domainName, pathOrUri, ct);
        }

        sw.Stop();
        _logger.LogInformation(
            "Parsed CSV {File} in {Elapsed}ms ({Mode} mode): {Ops} operations, {Schemas} schemas",
            Path.GetFileName(pathOrUri), sw.ElapsedMilliseconds,
            isFunctionCatalog ? "function catalog" : "generic data",
            descriptor.Operations.Count, descriptor.Schemas.Count);

        return descriptor;
    }

    private async Task<ServiceDescriptor> ParseFunctionCatalogAsync(
        CsvReader csv, string[] headers, string functionNameCol,
        string? categoryCol, string? subcategoryCol,
        string serviceName, string domainName, string filePath,
        CancellationToken ct)
    {
        var operations = new List<OperationDescriptor>();
        var tags = new Dictionary<string, TagDescriptor>(StringComparer.OrdinalIgnoreCase);

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var functionName = csv.GetField(functionNameCol);
            if (string.IsNullOrWhiteSpace(functionName)) continue;

            var category = categoryCol is not null ? csv.GetField(categoryCol) : null;
            var subcategory = subcategoryCol is not null ? csv.GetField(subcategoryCol) : null;

            var tag = category ?? "Default";
            var tagPascal = NamingHelper.ToPascalCase(tag);

            if (!tags.ContainsKey(tagPascal))
            {
                tags[tagPascal] = new TagDescriptor
                {
                    Name = tagPascal,
                    Description = subcategory is not null ? $"{tag} / {subcategory}" : tag
                };
            }

            var httpMethod = InferHttpMethodFromFunctionName(functionName);
            var operationId = NamingHelper.ToCamelCase(functionName);
            var kebabName = NamingHelper.ToKebabCase(functionName);

            operations.Add(new OperationDescriptor
            {
                OperationId = operationId,
                HttpMethod = httpMethod,
                Path = $"/api/{kebabName}",
                Summary = functionName,
                Description = BuildDescriptionFromRow(csv, headers, functionNameCol, categoryCol, subcategoryCol),
                Tag = tagPascal,
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["200"] = new() { StatusCode = "200", Description = "Success" }
                }
            });
        }

        return new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = filePath,
            Title = serviceName,
            Description = $"Function catalog parsed from {Path.GetFileName(filePath)}",
            Tags = tags.Values.ToList(),
            Operations = operations,
            Schemas = [],
            SecuritySchemes = ["BearerAuth"]
        };
    }

    private async Task<ServiceDescriptor> ParseGenericDataAsync(
        CsvReader csv, string[] headers,
        string serviceName, string domainName, string filePath,
        CancellationToken ct)
    {
        // Read sample rows for type inference
        var sampleRows = new List<Dictionary<string, string>>();
        var rowCount = 0;

        while (await csv.ReadAsync() && rowCount < 100)
        {
            ct.ThrowIfCancellationRequested();
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header) ?? string.Empty;
            }
            sampleRows.Add(row);
            rowCount++;
        }

        // Infer types from sample data
        var properties = headers.Select(header =>
        {
            var (type, format) = InferTypeFromSamples(sampleRows, header);
            return new Models.PropertyDescriptor
            {
                Name = NamingHelper.ToCamelCase(header),
                Type = type,
                Format = format,
                Description = header,
                Required = sampleRows.All(r => !string.IsNullOrWhiteSpace(r.GetValueOrDefault(header)))
            };
        }).ToList();

        var entityName = NamingHelper.ToPascalCase(serviceName);
        var entitySchema = new SchemaDescriptor
        {
            Name = entityName,
            Description = $"Entity from {Path.GetFileName(filePath)}",
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };

        var createSchema = new SchemaDescriptor
        {
            Name = $"Create{entityName}Request",
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };

        var updateSchema = new SchemaDescriptor
        {
            Name = $"Update{entityName}Request",
            Type = "object",
            Properties = properties.Select(p => p with { Required = false, IsNullable = true }).ToList()
        };

        var tag = new TagDescriptor { Name = entityName, Description = $"CRUD operations for {entityName}" };
        var kebab = NamingHelper.ToKebabCase(serviceName);

        var operations = new List<OperationDescriptor>
        {
            new()
            {
                OperationId = $"list{entityName}", HttpMethod = "GET", Path = $"/api/{kebab}",
                Summary = $"List all {entityName} records", Tag = entityName,
                QueryParameters =
                [
                    new ParameterDescriptor { Name = "page", Location = "query", Type = "integer", Format = "int32", DefaultValue = "1" },
                    new ParameterDescriptor { Name = "pageSize", Location = "query", Type = "integer", Format = "int32", DefaultValue = "20" }
                ],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"List of {entityName}" } }
            },
            new()
            {
                OperationId = $"get{entityName}", HttpMethod = "GET", Path = $"/api/{kebab}/{{id}}",
                Summary = $"Get a {entityName} by ID", Tag = entityName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"The {entityName}" }, ["404"] = new() { StatusCode = "404", Description = "Not found" } }
            },
            new()
            {
                OperationId = $"create{entityName}", HttpMethod = "POST", Path = $"/api/{kebab}",
                Summary = $"Create a new {entityName}", Tag = entityName,
                RequestBody = new SchemaDescriptor { Name = $"Create{entityName}Request" },
                Responses = new() { ["201"] = new() { StatusCode = "201", Description = $"Created {entityName}" } }
            },
            new()
            {
                OperationId = $"update{entityName}", HttpMethod = "PUT", Path = $"/api/{kebab}/{{id}}",
                Summary = $"Update an existing {entityName}", Tag = entityName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                RequestBody = new SchemaDescriptor { Name = $"Update{entityName}Request" },
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"Updated {entityName}" } }
            },
            new()
            {
                OperationId = $"delete{entityName}", HttpMethod = "DELETE", Path = $"/api/{kebab}/{{id}}",
                Summary = $"Delete a {entityName}", Tag = entityName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"Deleted {entityName}" } }
            }
        };

        return new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = filePath,
            Title = serviceName,
            Description = $"Data entity parsed from {Path.GetFileName(filePath)}",
            Tags = [tag],
            Operations = operations,
            Schemas = [entitySchema, createSchema, updateSchema],
            SecuritySchemes = ["BearerAuth"]
        };
    }

    private static (string Type, string? Format) InferTypeFromSamples(
        List<Dictionary<string, string>> samples, string column)
    {
        var values = samples
            .Select(r => r.GetValueOrDefault(column, ""))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Take(50)
            .ToList();

        if (values.Count == 0) return ("string", null);

        // Check if all values match a pattern
        if (values.All(v => int.TryParse(v, out _)))
            return ("integer", "int32");

        if (values.All(v => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _) && v.Contains('.')))
            return ("number", "double");

        if (values.All(v => v.Equals("true", StringComparison.OrdinalIgnoreCase)
                         || v.Equals("false", StringComparison.OrdinalIgnoreCase)))
            return ("boolean", null);

        if (values.All(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                         && v.Length > 8)) // Avoid matching short numbers as dates
            return ("string", "date-time");

        if (values.All(v => Guid.TryParse(v, out _)))
            return ("string", "uuid");

        return ("string", null);
    }

    private static string InferHttpMethodFromFunctionName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.StartsWith("get", StringComparison.Ordinal) || lower.StartsWith("list", StringComparison.Ordinal) || lower.StartsWith("find", StringComparison.Ordinal) ||
            lower.StartsWith("search", StringComparison.Ordinal) || lower.StartsWith("read", StringComparison.Ordinal) || lower.StartsWith("fetch", StringComparison.Ordinal))
            return "GET";
        if (lower.StartsWith("delete", StringComparison.Ordinal) || lower.StartsWith("remove", StringComparison.Ordinal))
            return "DELETE";
        if (lower.StartsWith("update", StringComparison.Ordinal) || lower.StartsWith("modify", StringComparison.Ordinal) || lower.StartsWith("set", StringComparison.Ordinal) ||
            lower.StartsWith("edit", StringComparison.Ordinal) || lower.StartsWith("change", StringComparison.Ordinal) || lower.StartsWith("replace", StringComparison.Ordinal))
            return "PUT";
        return "POST";
    }

    private static string BuildDescriptionFromRow(CsvReader csv, string[] headers,
        string functionNameCol, string? categoryCol, string? subcategoryCol)
    {
        var parts = new List<string>();
        foreach (var header in headers)
        {
            if (header == functionNameCol || header == categoryCol || header == subcategoryCol) continue;
            var value = csv.GetField(header);
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{header}: {value}");
        }
        return string.Join("; ", parts);
    }
}
