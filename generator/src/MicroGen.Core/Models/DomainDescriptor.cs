namespace MicroGen.Core.Models;

/// <summary>
/// Represents a top-level domain grouping (directory containing specs).
/// </summary>
public sealed record DomainDescriptor
{
    public required string Name { get; init; }
    public required string DirectoryPath { get; init; }
    public List<ServiceDescriptor> Services { get; init; } = [];

    public int TotalOperations => Services.Sum(s => s.TotalOperations);
    public int TotalSchemas => Services.Sum(s => s.Schemas.Count);

    // Computed naming
    public string DomainName => Name;
    public string PascalName => Helpers.NamingHelper.ToPascalCase(Name);
    public string KebabName => Helpers.NamingHelper.ToKebabCase(Name);
}
