using System.Text.RegularExpressions;

namespace MicroGen.Core.Helpers;

/// <summary>
/// Naming convention helpers for converting between OpenAPI naming and C# naming.
/// </summary>
public static partial class NamingHelper
{
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Split on non-alphanumeric chars, hyphens, underscores
        var parts = SplitRegex().Split(input)
            .Where(p => !string.IsNullOrEmpty(p));

        return string.Concat(parts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (pascal.Length == 0) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    public static string ToKebabCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Insert hyphen before uppercase letters and convert
        var kebab = KebabInsertRegex().Replace(input, "-$1");
        // Replace non-alphanumeric with hyphens
        kebab = NonAlphaRegex().Replace(kebab, "-");
        // Collapse multiple hyphens
        kebab = MultiHyphenRegex().Replace(kebab, "-");
        return kebab.Trim('-').ToLowerInvariant();
    }

    public static string ToNamespace(string domain, string service)
    {
        var d = ToPascalCase(domain);
        var s = ToPascalCase(service);
        return $"{s}";
    }

    /// <summary>
    /// Derives a clean service name from a filename.
    /// "auth-api.yaml" → "Auth", "user-profile-api.yaml" → "UserProfile"
    /// </summary>
    public static string ServiceNameFromFile(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        // Remove common suffixes
        name = ApiSuffixRegex().Replace(name, string.Empty);
        return ToPascalCase(name);
    }

    /// <summary>
    /// Generates an operationId if one is missing.
    /// </summary>
    public static string GenerateOperationId(string method, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !s.StartsWith('{'))
            .Select(ToPascalCase);
        var prefix = method.ToLowerInvariant() switch
        {
            "get" => "get",
            "post" => "create",
            "put" => "update",
            "patch" => "patch",
            "delete" => "delete",
            _ => method.ToLowerInvariant()
        };
        return prefix + string.Concat(segments);
    }

    [GeneratedRegex(@"[\s\-_./]+")]
    private static partial Regex SplitRegex();

    [GeneratedRegex(@"([A-Z])")]
    private static partial Regex KebabInsertRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex NonAlphaRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiHyphenRegex();

    [GeneratedRegex(@"[-_]?api$", RegexOptions.IgnoreCase)]
    private static partial Regex ApiSuffixRegex();
}
