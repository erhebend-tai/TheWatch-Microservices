namespace MicroGen.Core.Models;

/// <summary>
/// Represents an OpenAPI tag → maps to a controller.
/// </summary>
public sealed record TagDescriptor
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;

    public string PascalName => Helpers.NamingHelper.ToPascalCase(Name);
}
