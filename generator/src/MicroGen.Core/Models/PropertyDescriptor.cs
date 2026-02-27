namespace MicroGen.Core.Models;

/// <summary>
/// Represents a property within a schema.
/// </summary>
public sealed record PropertyDescriptor
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Format { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool Required { get; init; }
    public bool IsNullable { get; init; }
    public bool IsArray { get; init; }
    public string? ArrayItemType { get; init; }
    public string? RefSchemaName { get; init; }
    public string? Example { get; init; }

    // Validation
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }

    // Computed
    public string CSharpType
    {
        get
        {
            if (RefSchemaName is not null)
                return IsArray ? $"List<{Helpers.NamingHelper.ToPascalCase(RefSchemaName)}>" : Helpers.NamingHelper.ToPascalCase(RefSchemaName);

            if (IsArray)
                return $"List<{TypeMapping.MapToCSharp(ArrayItemType ?? "string", Format, true)}>";

            return TypeMapping.MapToCSharp(Type, Format, Required && !IsNullable);
        }
    }

    public string PascalName => Helpers.NamingHelper.ToPascalCase(Name);
    public string CamelName => Helpers.NamingHelper.ToCamelCase(Name);

    public bool HasValidation => MinLength.HasValue || MaxLength.HasValue
        || Pattern is not null || Minimum.HasValue || Maximum.HasValue;
}
