namespace MicroGen.Core.Models;

/// <summary>
/// Represents an OpenAPI schema → maps to a C# class/record/enum.
/// </summary>
public sealed record SchemaDescriptor
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "object";
    public string? Format { get; init; }
    public bool IsEnum { get; init; }
    public bool IsArray { get; init; }
    public bool IsNullable { get; init; }

    /// <summary>Schema name of array items when IsArray is true.</summary>
    public string? ArrayItemType { get; init; }

    /// <summary>Enum values when IsEnum is true.</summary>
    public List<string> EnumValues { get; init; } = [];

    /// <summary>Properties when Type is object.</summary>
    public List<PropertyDescriptor> Properties { get; init; } = [];

    /// <summary>Required property names.</summary>
    public List<string> RequiredProperties { get; init; } = [];

    /// <summary>Schemas this composes via allOf.</summary>
    public List<string> AllOf { get; init; } = [];

    // Computed
    public string PascalName => Helpers.NamingHelper.ToPascalCase(Name);
    public string CSharpType => IsEnum ? PascalName :
                                 IsArray ? $"List<{ArrayItemType ?? "object"}>" :
                                 Type == "object" ? PascalName :
                                 TypeMapping.MapToCSharp(Type, Format, !IsNullable);

    /// <summary>Whether this schema is a simple (entity-like) object vs a wrapper.</summary>
    public bool IsEntity => Type == "object" && !IsEnum && Properties.Count > 0
                            && !Name.EndsWith("Response", StringComparison.OrdinalIgnoreCase)
                            && !Name.EndsWith("Request", StringComparison.OrdinalIgnoreCase);
}
