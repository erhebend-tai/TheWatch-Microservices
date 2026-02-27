using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.SymbolIndexing;

/// <summary>
/// Generates LSIF (Language Server Index Format) data from a compiled .NET assembly
/// using reflection to extract types, members, and their relationships.
/// </summary>
public sealed class SymbolIndexGenerator
{
    private readonly ILogger<SymbolIndexGenerator> _logger;
    private Assembly? _assembly;
    private readonly Dictionary<string, LsifSymbol> _symbols = new();
    private readonly Dictionary<string, List<string>> _references = new();
    private int _elementIdCounter;

    public SymbolIndexGenerator(ILogger<SymbolIndexGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load an assembly and generate LSIF data.
    /// </summary>
    public async Task<LsifDocument> GenerateAsync(
        string assemblyPath,
        bool includeInternals = false,
        bool includePrivate = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Loading assembly: {Assembly}", Path.GetFileName(assemblyPath));

        try
        {
            _assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly: {Assembly}", assemblyPath);
            throw;
        }

        _logger.LogDebug("Extracting symbols from {Assembly}", _assembly.GetName().Name);

        var doc = new LsifDocument
        {
            Metadata = new LsifMetadata
            {
                ProjectRoot = Path.GetDirectoryName(assemblyPath) ?? ".",
                ToolInfo = new LsifToolInfo
                {
                    Name = "MicroGen.SymbolIndexer",
                    Version = "1.0.0"
                }
            }
        };

        // Extract all public types
        var types = _assembly.GetTypes()
            .Where(t => includeInternals || t.IsPublic)
            .OrderBy(t => t.FullName)
            .ToList();

        _logger.LogInformation("Indexing {Count} types...", types.Count);

        foreach (var type in types)
        {
            await IndexTypeAsync(type, includePrivate, doc, ct);
        }

        _logger.LogInformation("Generated {SymbolCount} symbols with {ReferenceCount} references",
            _symbols.Count, _references.Count);

        doc.Symbols = _symbols.Values.ToList();
        return doc;
    }

    private async Task IndexTypeAsync(
        Type type,
        bool includePrivate,
        LsifDocument doc,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var typeSymbol = new LsifSymbol
        {
            Id = GenerateElementId(),
            Name = type.Name,
            FullyQualifiedName = type.FullName ?? type.Name,
            Kind = GetSymbolKind(type),
            AccessLevel = GetAccessLevel(type),
            IsStatic = type.IsAbstract && type.IsSealed // static classes are abstract and sealed
        };

        // Add documentation if available
        if (TryGetDocumentation(type, out var docs))
        {
            typeSymbol.Documentation = docs;
        }

        // Index base types and interfaces
        if (type.BaseType != null && type.BaseType != typeof(object))
        {
            typeSymbol.BaseTypes.Add(type.BaseType.FullName ?? type.BaseType.Name);
        }

        foreach (var iface in type.GetInterfaces())
        {
            typeSymbol.ImplementedInterfaces.Add(iface.FullName ?? iface.Name);
        }

        // Add generic constraints if applicable
        if (type.IsGenericType)
        {
            foreach (var constraint in type.GetGenericArguments())
            {
                typeSymbol.GenericConstraints.Add(constraint.FullName ?? constraint.Name);
            }
        }

        _symbols[typeSymbol.FullyQualifiedName] = typeSymbol;

        // Index members
        var members = type.GetMembers(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            (includePrivate ? BindingFlags.NonPublic : 0))
            .Where(m => m.DeclaringType == type) // Only explicitly declared members
            .ToList();

        _logger.LogDebug("  Type {Type} has {MemberCount} members", type.Name, members.Count);

        foreach (var member in members)
        {
            IndexMember(member, typeSymbol.FullyQualifiedName, includePrivate);
        }

        await Task.Yield();
    }

    private void IndexMember(MemberInfo member, string parentType, bool includePrivate)
    {
        var symbolKind = GetMemberKind(member);
        if (symbolKind == null) return;

        var accessibility = GetMemberAccessLevel((MemberInfo?)member);
        if (!includePrivate && accessibility == "private") return;

        var fqn = $"{parentType}.{member.Name}";
        var symbol = new LsifSymbol
        {
            Id = GenerateElementId(),
            Name = member.Name,
            FullyQualifiedName = fqn,
            Kind = symbolKind,
            AccessLevel = accessibility
        };

        // Get method/property specifics
        if (member is MethodInfo method)
        {
            symbol.Signature = BuildMethodSignature(method);
            symbol.IsStatic = method.IsStatic;
            symbol.IsAbstract = method.IsAbstract;
            symbol.IsVirtual = method.IsVirtual;
            symbol.IsOverride = method.GetBaseDefinition() != method;
        }
        else if (member is PropertyInfo prop)
        {
            symbol.Signature = BuildPropertySignature(prop);
            symbol.IsStatic = (prop.GetGetMethod()?.IsStatic ?? false) ||
                             (prop.GetSetMethod()?.IsStatic ?? false);
        }

        // Documentation
        if (TryGetDocumentation(member, out var docs))
        {
            symbol.Documentation = docs;
        }

        _symbols[fqn] = symbol;

        _logger.LogDebug("    Indexed {Kind} {Member}", symbolKind, member.Name);
    }

    private string BuildMethodSignature(MethodInfo method)
    {
        var parameters = string.Join(", ", 
            method.GetParameters()
                .Select(p => $"{p.ParameterType.Name} {p.Name}"));
        
        return $"{method.ReturnType.Name} {method.Name}({parameters})";
    }

    private string BuildPropertySignature(PropertyInfo property)
    {
        var accessors = new List<string>();
        if (property.CanRead) accessors.Add("get");
        if (property.CanWrite) accessors.Add("set");
        
        return $"{property.PropertyType.Name} {property.Name} {{ {string.Join("/", accessors)} }}";
    }

    private string? GetSymbolKind(Type type)
    {
        if (type.IsInterface) return "interface";
        if (type.IsEnum) return "enum";
        if (type.IsValueType) return "struct";
        if (type.IsClass) return "class";
        return null;
    }

    private string? GetMemberKind(MemberInfo member)
    {
        return member.MemberType switch
        {
            MemberTypes.Method => "method",
            MemberTypes.Property => "property",
            MemberTypes.Field => "field",
            MemberTypes.Event => "event",
            MemberTypes.NestedType => member is Type t ? GetSymbolKind(t) : null,
            _ => null
        };
    }

    private string GetAccessLevel(Type type)
    {
        if (type.IsPublic) return "public";
        if (type.IsNestedPublic) return "public";
        if (type.IsNestedPrivate) return "private";
        if (type.IsNestedAssembly) return "internal";
        return "internal";
    }

    private string GetMemberAccessLevel(MemberInfo? member)
    {
        return member switch
        {
            MethodBase mb when mb.IsPublic => "public",
            MethodBase mb when mb.IsPrivate => "private",
            MethodBase mb when mb.IsAssembly => "internal",
            MethodBase mb when mb.IsFamily => "protected",
            PropertyInfo pi when (pi.GetGetMethod()?.IsPublic ?? false) || (pi.GetSetMethod()?.IsPublic ?? false) => "public",
            PropertyInfo pi when (pi.GetGetMethod()?.IsPrivate ?? false) || (pi.GetSetMethod()?.IsPrivate ?? false) => "private",
            _ => "internal"
        };
    }

    private bool TryGetDocumentation(MemberInfo member, out string documentation)
    {
        // In a real implementation, you'd load the XML documentation file
        // and extract the summary for this member. For now, we return empty.
        documentation = string.Empty;
        return false;
    }

    private string GenerateElementId() => $"element/{++_elementIdCounter}";
}

/// <summary>
/// LSIF Document structure
/// </summary>
public sealed class LsifDocument
{
    [JsonPropertyName("metadata")]
    public LsifMetadata Metadata { get; set; } = new();

    [JsonPropertyName("symbols")]
    public List<LsifSymbol> Symbols { get; set; } = new();

    [JsonPropertyName("references")]
    public Dictionary<string, List<LsifReference>> References { get; set; } = new();
}

public sealed class LsifMetadata
{
    [JsonPropertyName("projectRoot")]
    public string ProjectRoot { get; set; } = "";

    [JsonPropertyName("toolInfo")]
    public LsifToolInfo ToolInfo { get; set; } = new();

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LsifToolInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

public sealed class LsifSymbol
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("fullyQualifiedName")]
    public string FullyQualifiedName { get; set; } = "";

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("accessLevel")]
    public string AccessLevel { get; set; } = "internal";

    [JsonPropertyName("isStatic")]
    public bool IsStatic { get; set; }

    [JsonPropertyName("isAbstract")]
    public bool IsAbstract { get; set; }

    [JsonPropertyName("isVirtual")]
    public bool IsVirtual { get; set; }

    [JsonPropertyName("isOverride")]
    public bool IsOverride { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("documentation")]
    public string? Documentation { get; set; }

    [JsonPropertyName("baseTypes")]
    public List<string> BaseTypes { get; set; } = new();

    [JsonPropertyName("implementedInterfaces")]
    public List<string> ImplementedInterfaces { get; set; } = new();

    [JsonPropertyName("genericConstraints")]
    public List<string> GenericConstraints { get; set; } = new();
}

public sealed class LsifReference
{
    [JsonPropertyName("targetId")]
    public string TargetId { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "reference"; // reference, definition, implementation, override
}
