using System;
using System.Collections.Generic;
using System.Linq;

namespace TheWatch.Generators;

internal static class GeneratorHelpers
{
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
}
