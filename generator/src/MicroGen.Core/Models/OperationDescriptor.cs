namespace MicroGen.Core.Models;

/// <summary>
/// Represents a single API operation (one HTTP method on one path).
/// Maps to either a Command (POST/PUT/PATCH/DELETE) or Query (GET).
/// </summary>
public sealed record OperationDescriptor
{
    public required string OperationId { get; init; }
    public required string HttpMethod { get; init; }
    public required string Path { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;

    // Parameters
    public List<ParameterDescriptor> PathParameters { get; init; } = [];
    public List<ParameterDescriptor> QueryParameters { get; init; } = [];
    public List<ParameterDescriptor> HeaderParameters { get; init; } = [];

    // Request body
    public SchemaDescriptor? RequestBody { get; init; }
    public bool HasRequestBody => RequestBody is not null;

    // Responses
    public Dictionary<string, ResponseDescriptor> Responses { get; init; } = [];

    // Security
    public List<string> SecuritySchemes { get; init; } = [];
    public List<string> RequiredRoles { get; init; } = [];

    // Computed
    public bool IsQuery => HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
    public bool IsCommand => !IsQuery;

    public string PascalOperationId => Helpers.NamingHelper.ToPascalCase(OperationId);

    public string SuccessStatusCode => Responses.Keys
        .Where(k => k.StartsWith('2'))
        .OrderBy(k => k)
        .FirstOrDefault() ?? "200";

    public SchemaDescriptor? SuccessResponseSchema => Responses
        .Where(r => r.Key.StartsWith('2'))
        .Select(r => r.Value.Schema)
        .FirstOrDefault();
}

public sealed record ResponseDescriptor
{
    public required string StatusCode { get; init; }
    public string Description { get; init; } = string.Empty;
    public SchemaDescriptor? Schema { get; init; }
}
