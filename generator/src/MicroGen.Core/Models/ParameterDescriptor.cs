namespace MicroGen.Core.Models;

/// <summary>
/// Represents an API parameter (path, query, header).
/// </summary>
public sealed record ParameterDescriptor
{
    public required string Name { get; init; }
    public required string Location { get; init; } // path, query, header
    public required string Type { get; init; }
    public string Format { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }

    public string CSharpType => TypeMapping.MapToCSharp(Type, Format, Required);
    public string PascalName => Helpers.NamingHelper.ToPascalCase(Name);
    public string CamelName => Helpers.NamingHelper.ToCamelCase(Name);
}
