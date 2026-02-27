namespace MicroGen.Core.Models;

/// <summary>
/// Descriptor produced by parsing a single OpenAPI specification file.
/// Contains all metadata needed to generate a complete microservice.
/// </summary>
public sealed record ServiceDescriptor
{
    // Identity
    public required string DomainName { get; init; }
    public required string ServiceName { get; init; }
    public required string SpecFilePath { get; init; }

    // Metadata from info block
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }

    // Servers
    public List<ServerDescriptor> Servers { get; init; } = [];

    // Security
    public List<string> SecuritySchemes { get; init; } = [];

    // Tags → Controllers
    public List<TagDescriptor> Tags { get; init; } = [];

    // All operations across all paths
    public List<OperationDescriptor> Operations { get; init; } = [];

    // All schemas (component schemas)
    public List<SchemaDescriptor> Schemas { get; init; } = [];

    // Detected dependencies on other services
    public List<string> Dependencies { get; init; } = [];

    // Computed
    public int TotalOperations => Operations.Count;
    public int PathCount => Operations.Select(o => o.Path).Distinct().Count();

    public string PascalName => Helpers.NamingHelper.ToPascalCase(ServiceName);
    public string CamelName => Helpers.NamingHelper.ToCamelCase(ServiceName);
    public string KebabName => Helpers.NamingHelper.ToKebabCase(ServiceName);
}

public sealed record ServerDescriptor
{
    public required string Url { get; init; }
    public string Description { get; init; } = string.Empty;
}
