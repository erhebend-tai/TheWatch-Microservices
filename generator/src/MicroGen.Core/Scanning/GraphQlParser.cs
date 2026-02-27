using GraphQLParser;
using GraphQLParser.AST;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Parses GraphQL SDL schema files (.gql/.graphql) into <see cref="ServiceDescriptor"/> objects.
/// Maps types to schemas, queries to GET operations, and mutations to POST operations.
/// </summary>
public sealed class GraphQlParser : ISourceParser
{
    private readonly ILogger<GraphQlParser> _logger;

    public GraphQlParser(ILogger<GraphQlParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => [".gql", ".graphql"];

    public bool CanParse(string pathOrUri)
    {
        var ext = Path.GetExtension(pathOrUri);
        return SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ServiceDescriptor> ParseAsync(string pathOrUri, string domainName, CancellationToken ct = default)
    {
        _logger.LogDebug("Parsing GraphQL schema {File}", pathOrUri);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var sdlText = await File.ReadAllTextAsync(pathOrUri, ct).ConfigureAwait(false);
        var document = Parser.Parse(sdlText);

        var schemas = new List<SchemaDescriptor>();
        var operations = new List<OperationDescriptor>();
        var tags = new List<TagDescriptor>();
        var tableTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First pass: collect types that have @table directive (entities)
        foreach (var def in document.Definitions)
        {
            if (def is GraphQLObjectTypeDefinition typeDef && HasDirective(typeDef.Directives, "table"))
                tableTypes.Add(typeDef.Name.StringValue);
        }

        // Process all definitions
        foreach (var def in document.Definitions)
        {
            ct.ThrowIfCancellationRequested();

            switch (def)
            {
                case GraphQLObjectTypeDefinition typeDef:
                    ProcessObjectType(typeDef, schemas, operations, tags, tableTypes);
                    break;

                case GraphQLEnumTypeDefinition enumDef:
                    ProcessEnumType(enumDef, schemas);
                    break;

                case GraphQLInputObjectTypeDefinition inputDef:
                    ProcessInputType(inputDef, schemas);
                    break;
            }
        }

        sw.Stop();

        var serviceName = NamingHelper.ServiceNameFromFile(pathOrUri);

        var descriptor = new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = pathOrUri,
            Title = serviceName,
            Description = $"GraphQL schema from {Path.GetFileName(pathOrUri)}",
            Version = "1.0.0",
            Tags = tags,
            Operations = operations,
            Schemas = schemas,
            SecuritySchemes = ExtractSecuritySchemes(document)
        };

        _logger.LogInformation(
            "Parsed GraphQL {File} in {Elapsed}ms: {Types} schemas, {Ops} operations",
            Path.GetFileName(pathOrUri), sw.ElapsedMilliseconds, schemas.Count, operations.Count);

        return descriptor;
    }

    private static void ProcessObjectType(
        GraphQLObjectTypeDefinition typeDef,
        List<SchemaDescriptor> schemas,
        List<OperationDescriptor> operations,
        List<TagDescriptor> tags,
        HashSet<string> tableTypes)
    {
        var typeName = typeDef.Name.StringValue;

        // Skip Query/Mutation root types — process their fields as operations
        if (typeName is "Query" or "Mutation" or "Subscription")
        {
            if (typeDef.Fields is null) return;

            foreach (var field in typeDef.Fields)
            {
                var op = BuildOperationFromField(field, typeName);
                if (op is not null)
                    operations.Add(op);
            }
            return;
        }

        // Skip _expr types (Firebase DataConnect expressions)
        if (typeName.EndsWith("_expr", StringComparison.OrdinalIgnoreCase))
            return;

        var isEntity = tableTypes.Contains(typeName);
        var schema = BuildSchemaFromObjectType(typeDef);
        schemas.Add(schema);

        // Entity types get auto-CRUD operations and a tag
        if (isEntity)
        {
            tags.Add(new TagDescriptor
            {
                Name = typeName,
                Description = GetDescription(typeDef.Description) ?? $"CRUD operations for {typeName}"
            });

            operations.AddRange(BuildEntityCrudOperations(typeName));
        }
    }

    private static void ProcessEnumType(GraphQLEnumTypeDefinition enumDef, List<SchemaDescriptor> schemas)
    {
        var name = enumDef.Name.StringValue;
        if (name.EndsWith("_expr", StringComparison.OrdinalIgnoreCase)) return;

        var values = enumDef.Values?.Select(v => v.Name.StringValue).ToList() ?? [];

        schemas.Add(new SchemaDescriptor
        {
            Name = name,
            Description = GetDescription(enumDef.Description) ?? string.Empty,
            Type = "string",
            IsEnum = true,
            EnumValues = values
        });
    }

    private static void ProcessInputType(GraphQLInputObjectTypeDefinition inputDef, List<SchemaDescriptor> schemas)
    {
        var name = inputDef.Name.StringValue;
        if (name.EndsWith("_expr", StringComparison.OrdinalIgnoreCase)) return;

        var properties = inputDef.Fields?.Select(f =>
        {
            var (type, format, isArray, refSchema) = MapGraphQlType(f.Type);
            return new Models.PropertyDescriptor
            {
                Name = NamingHelper.ToCamelCase(f.Name.StringValue),
                Type = type,
                Format = format,
                Description = GetDescription(f.Description) ?? string.Empty,
                Required = f.Type is GraphQLNonNullType,
                IsArray = isArray,
                RefSchemaName = refSchema,
                ArrayItemType = isArray ? refSchema ?? type : null
            };
        }).ToList() ?? [];

        schemas.Add(new SchemaDescriptor
        {
            Name = name,
            Description = GetDescription(inputDef.Description) ?? string.Empty,
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        });
    }

    private static SchemaDescriptor BuildSchemaFromObjectType(GraphQLObjectTypeDefinition typeDef)
    {
        var properties = typeDef.Fields?.Where(f =>
            !f.Name.StringValue.EndsWith("_expr", StringComparison.OrdinalIgnoreCase))
            .Select(f =>
            {
                var (type, format, isArray, refSchema) = MapGraphQlType(f.Type);
                return new Models.PropertyDescriptor
                {
                    Name = NamingHelper.ToCamelCase(f.Name.StringValue),
                    Type = type,
                    Format = format,
                    Description = GetDescription(f.Description) ?? string.Empty,
                    Required = f.Type is GraphQLNonNullType,
                    IsArray = isArray,
                    RefSchemaName = refSchema,
                    ArrayItemType = isArray ? refSchema ?? type : null
                };
            }).ToList() ?? [];

        return new SchemaDescriptor
        {
            Name = typeDef.Name.StringValue,
            Description = GetDescription(typeDef.Description) ?? string.Empty,
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };
    }

    private static OperationDescriptor? BuildOperationFromField(
        GraphQLFieldDefinition field, string rootType)
    {
        var fieldName = field.Name.StringValue;
        var isQuery = rootType == "Query";
        var httpMethod = isQuery ? "GET" : "POST";

        var pascalName = NamingHelper.ToPascalCase(fieldName);
        var kebabName = NamingHelper.ToKebabCase(fieldName);
        var tag = "Default";

        // Infer tag from field name (e.g., listUsers → Users, createOrder → Orders)
        var resourceName = ExtractResourceName(fieldName);
        if (!string.IsNullOrEmpty(resourceName))
            tag = NamingHelper.ToPascalCase(resourceName);

        var parameters = new List<ParameterDescriptor>();
        SchemaDescriptor? requestBody = null;

        if (field.Arguments is { Count: > 0 })
        {
            if (isQuery)
            {
                // Query arguments → query parameters
                parameters = field.Arguments.Select(arg =>
                {
                    var (type, format, _, _) = MapGraphQlType(arg.Type);
                    return new ParameterDescriptor
                    {
                        Name = NamingHelper.ToCamelCase(arg.Name.StringValue),
                        Location = "query",
                        Type = type,
                        Format = format ?? string.Empty,
                        Required = arg.Type is GraphQLNonNullType
                    };
                }).ToList();
            }
            else
            {
                // Mutation arguments → request body
                var props = field.Arguments.Select(arg =>
                {
                    var (type, format, isArray, refSchema) = MapGraphQlType(arg.Type);
                    return new Models.PropertyDescriptor
                    {
                        Name = NamingHelper.ToCamelCase(arg.Name.StringValue),
                        Type = type,
                        Format = format,
                        Required = arg.Type is GraphQLNonNullType,
                        IsArray = isArray,
                        RefSchemaName = refSchema,
                        ArrayItemType = isArray ? refSchema ?? type : null
                    };
                }).ToList();

                requestBody = new SchemaDescriptor
                {
                    Name = $"{pascalName}Request",
                    Type = "object",
                    Properties = props,
                    RequiredProperties = props.Where(p => p.Required).Select(p => p.Name).ToList()
                };
            }
        }

        // Extract auth requirements from @auth directive
        var requiredRoles = ExtractAuthRoles(field.Directives);
        var securitySchemes = requiredRoles.Count > 0 ? new List<string> { "BearerAuth" } : [];

        return new OperationDescriptor
        {
            OperationId = NamingHelper.ToCamelCase(fieldName),
            HttpMethod = httpMethod,
            Path = $"/api/{kebabName}",
            Summary = GetDescription(field.Description) ?? fieldName,
            Tag = tag,
            QueryParameters = isQuery ? parameters : [],
            RequestBody = requestBody,
            SecuritySchemes = securitySchemes,
            RequiredRoles = requiredRoles,
            Responses = new Dictionary<string, ResponseDescriptor>
            {
                ["200"] = new() { StatusCode = "200", Description = "Success" }
            }
        };
    }

    private static List<OperationDescriptor> BuildEntityCrudOperations(string typeName)
    {
        var kebab = NamingHelper.ToKebabCase(typeName);
        var basePath = $"/api/{kebab}";

        return
        [
            new OperationDescriptor
            {
                OperationId = $"list{typeName}", HttpMethod = "GET", Path = basePath,
                Summary = $"List all {typeName} records", Tag = typeName,
                QueryParameters =
                [
                    new ParameterDescriptor { Name = "page", Location = "query", Type = "integer", Format = "int32", DefaultValue = "1" },
                    new ParameterDescriptor { Name = "pageSize", Location = "query", Type = "integer", Format = "int32", DefaultValue = "20" }
                ],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"List of {typeName}" } }
            },
            new OperationDescriptor
            {
                OperationId = $"get{typeName}", HttpMethod = "GET", Path = $"{basePath}/{{id}}",
                Summary = $"Get a {typeName} by ID", Tag = typeName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"The {typeName}" }, ["404"] = new() { StatusCode = "404", Description = "Not found" } }
            },
            new OperationDescriptor
            {
                OperationId = $"create{typeName}", HttpMethod = "POST", Path = basePath,
                Summary = $"Create a new {typeName}", Tag = typeName,
                RequestBody = new SchemaDescriptor { Name = $"Create{typeName}Request" },
                Responses = new() { ["201"] = new() { StatusCode = "201", Description = $"Created {typeName}" } }
            },
            new OperationDescriptor
            {
                OperationId = $"update{typeName}", HttpMethod = "PUT", Path = $"{basePath}/{{id}}",
                Summary = $"Update an existing {typeName}", Tag = typeName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                RequestBody = new SchemaDescriptor { Name = $"Update{typeName}Request" },
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"Updated {typeName}" } }
            },
            new OperationDescriptor
            {
                OperationId = $"delete{typeName}", HttpMethod = "DELETE", Path = $"{basePath}/{{id}}",
                Summary = $"Delete a {typeName}", Tag = typeName,
                PathParameters = [new ParameterDescriptor { Name = "id", Location = "path", Type = "string", Format = "uuid", Required = true }],
                Responses = new() { ["200"] = new() { StatusCode = "200", Description = $"Deleted {typeName}" } }
            }
        ];
    }

    private static (string Type, string? Format, bool IsArray, string? RefSchemaName) MapGraphQlType(GraphQLType? gqlType)
    {
        if (gqlType is null) return ("string", null, false, null);

        // Unwrap NonNull
        if (gqlType is GraphQLNonNullType nonNull)
            return MapGraphQlType(nonNull.Type);

        // List type
        if (gqlType is GraphQLListType listType)
        {
            var (innerType, innerFormat, _, innerRef) = MapGraphQlType(listType.Type);
            return ("array", null, true, innerRef ?? innerType);
        }

        // Named type
        if (gqlType is GraphQLNamedType namedType)
        {
            var name = namedType.Name.StringValue;
            return name switch
            {
                "String" => ("string", null, false, null),
                "Int" => ("integer", "int32", false, null),
                "Float" => ("number", "double", false, null),
                "Boolean" => ("boolean", null, false, null),
                "ID" => ("string", "uuid", false, null),
                "Date" => ("string", "date", false, null),
                "DateTime" or "Timestamp" => ("string", "date-time", false, null),
                "UUID" => ("string", "uuid", false, null),
                "Any" or "JSON" => ("object", null, false, null),
                _ => ("object", null, false, name) // Custom type → reference
            };
        }

        return ("string", null, false, null);
    }

    private static bool HasDirective(GraphQLDirectives? directives, string name) =>
        directives?.Any(d => d.Name.StringValue.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? false;

    private static List<string> ExtractAuthRoles(GraphQLDirectives? directives)
    {
        if (directives is null) return [];

        var authDirective = directives.FirstOrDefault(d =>
            d.Name.StringValue.Equals("auth", StringComparison.OrdinalIgnoreCase));
        if (authDirective?.Arguments is null) return [];

        var levelArg = authDirective.Arguments.FirstOrDefault(a =>
            a.Name.StringValue.Equals("level", StringComparison.OrdinalIgnoreCase));
        if (levelArg?.Value is GraphQLEnumValue enumVal)
            return [enumVal.Name.StringValue];
        if (levelArg?.Value is GraphQLStringValue strVal)
            return [strVal.Value.ToString()];

        return [];
    }

    private static List<string> ExtractSecuritySchemes(GraphQLDocument document)
    {
        // Check if any operation has @auth directive
        foreach (var def in document.Definitions)
        {
            if (def is GraphQLObjectTypeDefinition { Name.StringValue: "Query" or "Mutation" } typeDef)
            {
                if (typeDef.Fields?.Any(f => HasDirective(f.Directives, "auth")) == true)
                    return ["BearerAuth"];
            }
        }
        return [];
    }

    private static string? GetDescription(GraphQLDescription? desc) =>
        desc?.Value.Length > 0 ? desc.Value.ToString() : null;

    private static string ExtractResourceName(string fieldName)
    {
        // Strip common prefixes: list, get, create, update, delete, find, search
        var prefixes = new[] { "list", "get", "create", "update", "delete", "find", "search", "remove", "add" };
        var lower = fieldName;
        foreach (var prefix in prefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && lower.Length > prefix.Length)
            {
                var rest = fieldName[prefix.Length..];
                return rest;
            }
        }
        return fieldName;
    }
}
