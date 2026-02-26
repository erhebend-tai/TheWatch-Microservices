using System;
using System.Collections.Generic;
using System.Linq;

namespace TheWatch.Generators;

internal static class GeneratorHelpers
{
    // Known non-P# service names that the generators should recognize
    private static readonly HashSet<string> KnownServiceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Geospatial"
    };

    internal static string? DetectProgram(string projectName)
    {
        // Extract P1-P10 from project name like TheWatch.P2.VoiceEmergency
        foreach (var part in projectName.Split('.'))
        {
            if (part.Length >= 2 && part[0] == 'P' && char.IsDigit(part[1]))
            {
                if (part.Length == 2 || (part.Length == 3 && char.IsDigit(part[2])))
                    return part;
            }
            if (KnownServiceNames.Contains(part))
                return part;
        }
        return null;
    }

    internal static string PascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p =>
            char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : "")));
    }

    internal static string CamelCase(string input)
    {
        var pascal = PascalCase(input);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + (pascal.Length > 1 ? pascal.Substring(1) : "");
    }

    internal static string MapJsonTypeToCSharp(string jsonType)
    {
        switch (jsonType?.ToLowerInvariant())
        {
            case "string": return "string";
            case "integer": return "int";
            case "number": return "double";
            case "boolean": return "bool";
            case "array": return "List<object>";
            case "object": return "object";
            default: return "object";
        }
    }

    // Well-known C# types that we can safely emit
    private static readonly HashSet<string> KnownBaseTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "string", "int", "long", "short", "byte", "float", "double", "decimal",
        "bool", "char", "object", "dynamic", "void",
        "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "Uri",
        "byte[]", "int?", "long?", "short?", "byte?", "float?", "double?",
        "decimal?", "bool?", "char?", "DateTime?", "DateTimeOffset?", "TimeSpan?", "Guid?"
    };

    internal static string CleanPropertyType(string rawType)
    {
        if (string.IsNullOrEmpty(rawType)) return "string";
        var t = rawType;
        // Remove everything before the last newline (comments, attributes)
        var nlIdx = t.LastIndexOf('\n');
        if (nlIdx >= 0) t = t.Substring(nlIdx + 1);
        t = t.Trim();
        // Remove access modifiers
        foreach (var mod in new[] { "public ", "private ", "protected ", "internal ", "static ", "readonly ", "virtual ", "override ", "abstract ", "sealed ", "required " })
        {
            while (t.StartsWith(mod))
                t = t.Substring(mod.Length);
        }
        // Remove attribute prefixes like [Key]
        while (t.StartsWith("["))
        {
            var end = t.IndexOf(']');
            if (end < 0) break;
            t = t.Substring(end + 1).TrimStart();
        }
        t = t.Trim();
        if (string.IsNullOrEmpty(t)) return "object";

        // Handle nullable
        var isNullable = t.EndsWith("?");
        var baseType = isNullable ? t.Substring(0, t.Length - 1) : t;

        // Handle List<T>, Dictionary<K,V>, IEnumerable<T>, etc
        if (baseType.StartsWith("List<") || baseType.StartsWith("IList<") ||
            baseType.StartsWith("IEnumerable<") || baseType.StartsWith("ICollection<") ||
            baseType.StartsWith("IReadOnlyList<") || baseType.StartsWith("HashSet<"))
            return "List<object>" + (isNullable ? "?" : "");

        if (baseType.StartsWith("Dictionary<") || baseType.StartsWith("IDictionary<") ||
            baseType.StartsWith("IReadOnlyDictionary<") || baseType.StartsWith("ConcurrentDictionary<"))
            return "Dictionary<string, object>" + (isNullable ? "?" : "");

        if (baseType.StartsWith("Task<"))
            return "Task<object>" + (isNullable ? "?" : "");
        if (baseType == "Task") return "Task";

        // Known simple types pass through
        if (KnownBaseTypes.Contains(t)) return t;
        if (KnownBaseTypes.Contains(baseType)) return t; // with nullable

        // Arrays
        if (t.EndsWith("[]"))
        {
            var elemType = t.Substring(0, t.Length - 2);
            if (KnownBaseTypes.Contains(elemType)) return t;
            return "object[]";
        }

        // Unknown custom types -> object
        return "object" + (isNullable ? "?" : "");
    }

    internal static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        // Remove invalid chars
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (cleaned.Length == 0) return "Unknown";
        if (char.IsDigit(cleaned[0])) cleaned = "_" + cleaned;
        return cleaned;
    }

    internal static string ExtractOperationName(string operationId, string method, string path)
    {
        if (!string.IsNullOrEmpty(operationId))
            return PascalCase(SanitizeIdentifier(operationId));

        // Build from method + path segments
        var segments = path.Split('/')
            .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("{"))
            .Select(s => PascalCase(s));
        var name = PascalCase(method.ToLowerInvariant()) + string.Join("", segments);
        return SanitizeIdentifier(name);
    }

    /// <summary>
    /// Parses parameter types like "[FromBody] RegisterRequest" into (attributeName, typeName).
    /// Returns (null, cleanedType) if no binding attribute is present.
    /// </summary>
    internal static (string Attribute, string TypeName) ExtractFromAttribute(string paramType)
    {
        if (string.IsNullOrEmpty(paramType))
            return (null, "object");

        var t = paramType.Trim();
        string attr = null;

        // Strip all bracket attributes, only keeping From* as the binding attribute
        while (t.StartsWith("["))
        {
            var end = t.IndexOf(']');
            if (end < 0) break;
            var attrContent = t.Substring(1, end - 1); // e.g. "FromBody", "Required"
            t = t.Substring(end + 1).Trim();

            // Only recognize From* attributes as binding attributes
            if (attrContent.StartsWith("From"))
                attr = attrContent;
        }

        // Remove newlines and attribute noise (e.g. "[Required]\n public string")
        var nlIdx = t.LastIndexOf('\n');
        if (nlIdx >= 0) t = t.Substring(nlIdx + 1).Trim();

        // Clean the type name — remove modifiers but keep the type identifier
        foreach (var mod in new[] { "public ", "private ", "protected ", "internal ", "static ", "readonly ", "virtual ", "override ", "abstract ", "sealed ", "required " })
        {
            while (t.StartsWith(mod))
                t = t.Substring(mod.Length);
        }

        // Strip any remaining bracket attributes after newline processing
        while (t.StartsWith("["))
        {
            var end = t.IndexOf(']');
            if (end < 0) break;
            t = t.Substring(end + 1).Trim();
        }

        if (string.IsNullOrEmpty(t)) t = "object";

        return (attr, t);
    }

    /// <summary>
    /// Extracts the HTTP method attribute name from attributes like "HttpPost(\"register\")" or "HttpGet".
    /// Returns (httpVerb, routeArg) e.g. ("HttpPost", "register") or ("HttpGet", null).
    /// </summary>
    internal static (string Verb, string Route) ParseHttpAttribute(string attr)
    {
        if (string.IsNullOrEmpty(attr)) return (null, null);

        var parenIdx = attr.IndexOf('(');
        if (parenIdx < 0)
            return (attr, null);

        var verb = attr.Substring(0, parenIdx);
        // Extract route from HttpPost("register") → register
        var routeStart = attr.IndexOf('"', parenIdx);
        if (routeStart < 0) return (verb, null);
        var routeEnd = attr.IndexOf('"', routeStart + 1);
        if (routeEnd < 0) return (verb, null);
        return (verb, attr.Substring(routeStart + 1, routeEnd - routeStart - 1));
    }
}
