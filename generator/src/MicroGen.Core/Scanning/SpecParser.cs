using System.Text.Json.Nodes;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using Microsoft.OpenApi.Reader;
using YamlDotNet.Serialization;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Parses a single OpenAPI specification file into a ServiceDescriptor.
/// Uses Microsoft.OpenApi 2.0.0-preview.11+ API surface.
/// </summary>
public sealed class SpecParser : ISourceParser
{
    private readonly ILogger<SpecParser> _logger;

    public SpecParser(ILogger<SpecParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => [".yaml", ".yml", ".json"];

    public bool CanParse(string pathOrUri)
    {
        var ext = Path.GetExtension(pathOrUri);
        return SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ServiceDescriptor> ParseAsync(
        string pathOrUri,
        string domainName,
        CancellationToken ct = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["SpecFile"] = Path.GetFileName(pathOrUri),
            ["Domain"] = domainName
        });

        _logger.LogDebug("Parsing API specification from {FilePath}", pathOrUri);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Stream parseStream;

        if (pathOrUri.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
            pathOrUri.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            // Convert YAML to JSON in-memory since Microsoft.OpenApi v2 preview
            // doesn't reliably load YAML via the format parameter
            var yamlText = await File.ReadAllTextAsync(pathOrUri, ct).ConfigureAwait(false);
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(yamlText));
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
            var jsonText = serializer.Serialize(yamlObject ?? new object());
            parseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonText));
        }
        else
        {
            parseStream = File.OpenRead(pathOrUri);
        }

        await using var _ = parseStream;
        var readResult = await OpenApiDocument.LoadAsync(parseStream, "json", cancellationToken: ct)
            .ConfigureAwait(false);

        var doc = readResult.Document;
        var diagnostic = readResult.Diagnostic;

        if (diagnostic?.Errors?.Count > 0)
        {
            foreach (var error in diagnostic.Errors)
            {
                _logger.LogWarning("OpenAPI Issue in {File}: {InternalLocation} - {Error}",
                    pathOrUri, error.Pointer, error.Message);
            }
        }

        var serviceName = NamingHelper.ServiceNameFromFile(pathOrUri);

        var descriptor = new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = pathOrUri,
            Title = doc.Info?.Title ?? serviceName,
            Description = doc.Info?.Description ?? string.Empty,
            Version = doc.Info?.Version ?? "1.0.0",
            ContactName = doc.Info?.Contact?.Name,
            ContactEmail = doc.Info?.Contact?.Email,
            Servers = ParseServers(doc),
            SecuritySchemes = ParseSecuritySchemes(doc),
            Tags = ParseTags(doc),
            Operations = ParseOperations(doc),
            Schemas = ParseSchemas(doc)
        };

        sw.Stop();
        _logger.LogInformation("Parsed {Service} ({Title} v{Version}) in {Elapsed}ms. Found {OpCount} ops, {SchemaCount} schemas.",
            serviceName, descriptor.Title, descriptor.Version, sw.ElapsedMilliseconds, descriptor.Operations.Count, descriptor.Schemas.Count);

        return descriptor;
    }

    private static List<ServerDescriptor> ParseServers(OpenApiDocument doc)
    {
        return doc.Servers?.Select(s => new ServerDescriptor
        {
            Url = s.Url,
            Description = s.Description ?? string.Empty
        }).ToList() ?? [];
    }

    private static List<string> ParseSecuritySchemes(OpenApiDocument doc)
    {
        return doc.Components?.SecuritySchemes?.Keys.ToList() ?? [];
    }

    private static List<TagDescriptor> ParseTags(OpenApiDocument doc)
    {
        var tags = doc.Tags?.Select(t => new TagDescriptor
        {
            Name = t.Name,
            Description = t.Description ?? string.Empty
        }).ToList() ?? [];

        // Also discover tags used in operations but not declared
        if (doc.Paths is not null)
        {
            var usedTags = doc.Paths
                .SelectMany(p => p.Value.Operations)
                .SelectMany(op => op.Value.Tags ?? (IEnumerable<OpenApiTagReference>)[])
                .Select(t => t.Name)
                .Distinct()
                .Where(t => !tags.Any(existing => existing.Name.Equals(t, StringComparison.OrdinalIgnoreCase)));

            foreach (var tagName in usedTags)
            {
                tags.Add(new TagDescriptor { Name = tagName });
            }
        }

        return tags;
    }

    private static List<OperationDescriptor> ParseOperations(OpenApiDocument doc)
    {
        var operations = new List<OperationDescriptor>();

        if (doc.Paths is null) return operations;

        foreach (var (path, pathItem) in doc.Paths)
        {
            foreach (var (method, operation) in pathItem.Operations)
            {
                var httpMethod = method.ToString().ToUpperInvariant();
                var operationId = operation.OperationId
                    ?? NamingHelper.GenerateOperationId(httpMethod, path);

                var tag = operation.Tags?.FirstOrDefault()?.Name ?? "Default";

                var op = new OperationDescriptor
                {
                    OperationId = operationId,
                    HttpMethod = httpMethod,
                    Path = path,
                    Summary = operation.Summary ?? string.Empty,
                    Description = operation.Description ?? string.Empty,
                    Tag = tag,
                    PathParameters = ParseParameters(operation.Parameters, ParameterLocation.Path),
                    QueryParameters = ParseParameters(operation.Parameters, ParameterLocation.Query),
                    HeaderParameters = ParseParameters(operation.Parameters, ParameterLocation.Header),
                    RequestBody = ParseRequestBody(operation.RequestBody),
                    Responses = ParseResponses(operation.Responses),
                    SecuritySchemes = operation.Security?
                        .SelectMany(s => s.Keys.Select(k => k.Name))
                        .ToList() ?? [],
                    RequiredRoles = ParseRequiredRoles(operation.Extensions)
                };

                operations.Add(op);
            }
        }

        return operations;
    }

    private static List<ParameterDescriptor> ParseParameters(
        IList<IOpenApiParameter>? parameters, ParameterLocation location)
    {
        if (parameters is null) return [];

        return parameters
            .Where(p => p.In == location)
            .Select(p => new ParameterDescriptor
            {
                Name = p.Name,
                Location = location.ToString().ToLowerInvariant(),
                Type = JsonSchemaTypeToString(p.Schema?.Type),
                Format = p.Schema?.Format ?? string.Empty,
                Description = p.Description ?? string.Empty,
                Required = p.Required,
                DefaultValue = p.Schema?.Default?.ToString()
            })
            .ToList();
    }

    private static SchemaDescriptor? ParseRequestBody(IOpenApiRequestBody? body)
    {
        if (body is null) return null;

        var content = body.Content?
            .Where(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .FirstOrDefault();

        if (content?.Schema is null) return null;

        return MapSchema("RequestBody", content.Schema);
    }

    private static Dictionary<string, ResponseDescriptor> ParseResponses(OpenApiResponses? responses)
    {
        if (responses is null) return [];

        var result = new Dictionary<string, ResponseDescriptor>();

        foreach (var (statusCode, response) in responses)
        {
            var schema = response.Content?
                .Where(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value.Schema)
                .FirstOrDefault();

            result[statusCode] = new ResponseDescriptor
            {
                StatusCode = statusCode,
                Description = response.Description ?? string.Empty,
                Schema = schema is not null ? MapSchema($"Response{statusCode}", schema) : null
            };
        }

        return result;
    }

    private static List<SchemaDescriptor> ParseSchemas(OpenApiDocument doc)
    {
        if (doc.Components?.Schemas is null) return [];

        return doc.Components.Schemas
            .Select(kvp => MapSchema(kvp.Key, kvp.Value))
            .ToList();
    }

    private static SchemaDescriptor MapSchema(string name, IOpenApiSchema schema)
    {
        var typeStr = JsonSchemaTypeToString(schema.Type);
        var isNullable = schema.Type?.HasFlag(JsonSchemaType.Null) ?? false;
        var itemsRefId = GetSchemaRefId(schema.Items);

        return new SchemaDescriptor
        {
            Name = name,
            Description = schema.Description ?? string.Empty,
            Type = typeStr,
            Format = schema.Format,
            IsEnum = schema.Enum?.Count > 0,
            IsArray = typeStr == "array",
            IsNullable = isNullable,
            ArrayItemType = itemsRefId ?? JsonSchemaTypeToString(schema.Items?.Type),
            EnumValues = schema.Enum?.Select(e => e.ToString()).ToList() ?? [],
            Properties = schema.Properties?.Select(p => MapProperty(p.Key, p.Value,
                schema.Required?.Contains(p.Key) ?? false)).ToList() ?? [],
            RequiredProperties = schema.Required?.ToList() ?? [],
            AllOf = schema.AllOf?
                .Select(GetSchemaRefId)
                .Where(id => id is not null)
                .Select(id => id!)
                .ToList() ?? []
        };
    }

    private static Models.PropertyDescriptor MapProperty(string name, IOpenApiSchema schema, bool required)
    {
        var typeStr = JsonSchemaTypeToString(schema.Type);
        var isNullable = schema.Type?.HasFlag(JsonSchemaType.Null) ?? false;
        var refId = GetSchemaRefId(schema);
        var itemsRefId = GetSchemaRefId(schema.Items);

        return new Models.PropertyDescriptor
        {
            Name = name,
            Type = typeStr,
            Format = schema.Format,
            Description = schema.Description ?? string.Empty,
            Required = required,
            IsNullable = isNullable,
            IsArray = typeStr == "array",
            ArrayItemType = itemsRefId ?? JsonSchemaTypeToString(schema.Items?.Type),
            RefSchemaName = refId,
            Example = schema.Example?.ToString(),
            MinLength = schema.MinLength > 0 ? (int)schema.MinLength : null,
            MaxLength = schema.MaxLength > 0 ? (int)schema.MaxLength : null,
            Pattern = schema.Pattern,
            Minimum = schema.Minimum.HasValue ? (double)schema.Minimum.Value : null,
            Maximum = schema.Maximum.HasValue ? (double)schema.Maximum.Value : null
        };
    }

    private static List<string> ParseRequiredRoles(IDictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>? extensions)
    {
        // Parse x-required-roles extension
        if (extensions is null) return [];

        if (extensions.TryGetValue("x-required-roles", out var ext))
        {
            // Simple string parsing from extension
            return ext.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList() ?? [];
        }

        return [];
    }

    /// <summary>
    /// Extracts the reference ID from a schema, if it is a schema reference.
    /// </summary>
    private static string? GetSchemaRefId(IOpenApiSchema? schema)
    {
        if (schema is OpenApiSchemaReference schemaRef)
            return schemaRef.Reference?.Id;
        return null;
    }

    /// <summary>
    /// Converts <see cref="JsonSchemaType"/> flags to a simple string representation.
    /// </summary>
    private static string JsonSchemaTypeToString(JsonSchemaType? type)
    {
        if (type is null) return "object";

        // Strip the Null flag for the base type
        var baseType = type.Value & ~JsonSchemaType.Null;

        return baseType switch
        {
            JsonSchemaType.String => "string",
            JsonSchemaType.Integer => "integer",
            JsonSchemaType.Number => "number",
            JsonSchemaType.Boolean => "boolean",
            JsonSchemaType.Array => "array",
            JsonSchemaType.Object => "object",
            _ => "object"
        };
    }
}
