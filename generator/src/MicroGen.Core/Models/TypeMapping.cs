namespace MicroGen.Core.Models;

/// <summary>
/// Maps OpenAPI types/formats to C# types.
/// </summary>
public static class TypeMapping
{
    public static string MapToCSharp(string? openApiType, string? format, bool required)
    {
        var baseType = (openApiType?.ToLowerInvariant(), format?.ToLowerInvariant()) switch
        {
            ("string", "date-time") => "DateTime",
            ("string", "date") => "DateOnly",
            ("string", "time") => "TimeOnly",
            ("string", "uuid") => "Guid",
            ("string", "uri") => "Uri",
            ("string", "email") => "string",
            ("string", "binary") => "byte[]",
            ("string", "byte") => "byte[]",
            ("string", "duration") => "TimeSpan",
            ("string", _) => "string",
            ("integer", "int64") => "long",
            ("integer", _) => "int",
            ("number", "float") => "float",
            ("number", "double") => "double",
            ("number", "decimal") => "decimal",
            ("number", _) => "double",
            ("boolean", _) => "bool",
            ("array", _) => "List<object>",
            ("object", _) => "object",
            _ => "string"
        };

        // Value types get nullable suffix when not required
        if (!required && IsValueType(baseType))
            return baseType + "?";

        return baseType;
    }

    private static bool IsValueType(string type) => type switch
    {
        "int" or "long" or "float" or "double" or "decimal" or "bool"
            or "DateTime" or "DateOnly" or "TimeOnly" or "Guid" or "TimeSpan" => true,
        _ => false
    };
}
