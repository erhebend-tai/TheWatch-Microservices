using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.SymbolIndexing;

/// <summary>
/// Provides symbol lookup and navigation queries against an LSIF document.
/// Implements operations for find-definition, find-references, hover, hierarchy, etc.
/// </summary>
public sealed class SymbolIndexQueryService
{
    private readonly ILogger<SymbolIndexQueryService> _logger;
    private LsifDocument? _index;

    public SymbolIndexQueryService(ILogger<SymbolIndexQueryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load an LSIF document for querying.
    /// </summary>
    public void LoadIndex(LsifDocument document)
    {
        _logger.LogInformation("Loading symbol index with {SymbolCount} symbols", 
            document.Symbols.Count);
        _index = document;
    }

    /// <summary>
    /// Find symbol by fully qualified name or partial name.
    /// </summary>
    public LsifSymbol? FindSymbol(string query)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        // Exact match first
        var exact = _index.Symbols.FirstOrDefault(s => 
            s.FullyQualifiedName.Equals(query, StringComparison.OrdinalIgnoreCase));
        
        if (exact != null) return exact;

        // Partial name match on simple name
        var partial = _index.Symbols.FirstOrDefault(s => 
            s.Name.Equals(query, StringComparison.OrdinalIgnoreCase));
        
        return partial;
    }

    /// <summary>
    /// Find all symbols in a specific namespace/type.
    /// </summary>
    public List<LsifSymbol> FindSymbolsIn(string namespaceName)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        return _index.Symbols
            .Where(s => s.FullyQualifiedName.StartsWith(namespaceName, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Find all references to a symbol.
    /// </summary>
    public List<LsifSymbol> FindReferences(string fullyQualifiedName)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        _logger.LogDebug("Finding references to {Symbol}", fullyQualifiedName);

        // Find all symbols that reference the target
        var references = new List<LsifSymbol>();

        foreach (var symbol in _index.Symbols)
        {
            // Check if this symbol references the target
            if (IsSymbolReferencing(symbol, fullyQualifiedName))
            {
                references.Add(symbol);
            }
        }

        _logger.LogDebug("Found {ReferenceCount} references to {Symbol}", 
            references.Count, fullyQualifiedName);

        return references;
    }

    /// <summary>
    /// Find all implementations of a type or interface.
    /// </summary>
    public List<LsifSymbol> FindImplementations(string typeName)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        _logger.LogDebug("Finding implementations of {Type}", typeName);

        return _index.Symbols
            .Where(s => 
                s.ImplementedInterfaces.Any(i => i.Equals(typeName, StringComparison.OrdinalIgnoreCase)) ||
                s.BaseTypes.Any(b => b.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Get type hierarchy for a type (base types and derived types).
    /// </summary>
    public TypeHierarchy GetTypeHierarchy(string typeFullName)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        var type = FindSymbol(typeFullName);
        if (type == null || !new[] { "class", "struct", "interface" }.Contains(type.Kind))
            throw new ArgumentException($"Type not found: {typeFullName}");

        var hierarchy = new TypeHierarchy
        {
            Type = type,
            BaseTypes = ResolveBaseHierarchy(type),
            DerivedTypes = ResolveDerivedTypes(type),
            ImplementingTypes = ResolveImplementingTypes(type)
        };

        _logger.LogDebug("Built hierarchy for {Type}: {BaseCount} bases, {DerivedCount} derived",
            type.Name, hierarchy.BaseTypes.Count, hierarchy.DerivedTypes.Count);

        return hierarchy;
    }

    /// <summary>
    /// Get all members of a type with option to include inherited members.
    /// </summary>
    public List<LsifSymbol> GetTypeMembers(string typeFullName, bool includeInherited = false)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        var members = _index.Symbols
            .Where(s => s.FullyQualifiedName.StartsWith(typeFullName + ".", StringComparison.Ordinal))
            .ToList();

        if (includeInherited)
        {
            var type = FindSymbol(typeFullName);
            if (type != null)
            {
                // Add members from base types
                foreach (var baseType in ResolveBases(type))
                {
                    members.AddRange(_index.Symbols
                        .Where(s => s.FullyQualifiedName.StartsWith(baseType.FullyQualifiedName + ".", StringComparison.Ordinal))
                        .Where(s => s.AccessLevel != "private"));
                }
            }
        }

        _logger.LogDebug("Retrieved {MemberCount} members for {Type}", members.Count, typeFullName);
        return members;
    }

    /// <summary>
    /// Generate hover information for a symbol.
    /// </summary>
    public HoverInformation GetHoverInfo(string fullyQualifiedName)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        var symbol = FindSymbol(fullyQualifiedName);
        if (symbol == null)
            return new HoverInformation { Content = "Symbol not found" };

        var hover = new HoverInformation
        {
            Content = BuildHoverContent(symbol),
            DocumentationUrl = null // Could link to docs site
        };

        return hover;
    }

    /// <summary>
    /// Search for symbols by various criteria.
    /// </summary>
    public List<LsifSymbol> Search(SymbolSearchCriteria criteria)
    {
        if (_index == null)
            throw new InvalidOperationException("Index not loaded");

        var results = _index.Symbols.AsEnumerable();

        if (!string.IsNullOrEmpty(criteria.NamePattern))
        {
            results = results.Where(s => 
                s.Name.Contains(criteria.NamePattern, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(criteria.Kind))
        {
            results = results.Where(s => 
                s.Kind != null && s.Kind.Equals(criteria.Kind, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(criteria.AccessLevel))
        {
            results = results.Where(s => 
                s.AccessLevel.Equals(criteria.AccessLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.IsStatic.HasValue)
        {
            results = results.Where(s => s.IsStatic == criteria.IsStatic.Value);
        }

        if (criteria.IsAbstract.HasValue)
        {
            results = results.Where(s => s.IsAbstract == criteria.IsAbstract.Value);
        }

        var list = results.ToList();
        _logger.LogDebug("Search returned {ResultCount} results", list.Count);
        return list;
    }

    // --- Private Helpers ---

    private bool IsSymbolReferencing(LsifSymbol symbol, string targetFqn)
    {
        // Check if base types reference target
        if (symbol.BaseTypes.Any(b => b.Equals(targetFqn, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check if implemented interfaces reference target
        if (symbol.ImplementedInterfaces.Any(i => i.Equals(targetFqn, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check if generic constraints reference target
        if (symbol.GenericConstraints.Any(g => g.Equals(targetFqn, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check if signature contains reference to target
        if (!string.IsNullOrEmpty(symbol.Signature) && 
            symbol.Signature.Contains(ExtractTypeName(targetFqn), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private List<LsifSymbol> ResolveBases(LsifSymbol type)
    {
        var bases = new List<LsifSymbol>();
        
        foreach (var baseName in type.BaseTypes)
        {
            var base_ = FindSymbol(baseName);
            if (base_ != null)
            {
                bases.Add(base_);
                bases.AddRange(ResolveBases(base_));
            }
        }

        return bases;
    }

    private List<LsifSymbol> ResolveBaseHierarchy(LsifSymbol type)
    {
        return ResolveBases(type).Distinct().ToList();
    }

    private List<LsifSymbol> ResolveDerivedTypes(LsifSymbol type)
    {
        if (_index == null) return new();

        return _index.Symbols
            .Where(s => s.BaseTypes.Any(b => 
                b.Equals(type.FullyQualifiedName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private List<LsifSymbol> ResolveImplementingTypes(LsifSymbol type)
    {
        if (_index == null) return new();

        return _index.Symbols
            .Where(s => s.ImplementedInterfaces.Any(i => 
                i.Equals(type.FullyQualifiedName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private string BuildHoverContent(LsifSymbol symbol)
    {
        var lines = new List<string>();

        // Kind + name
        lines.Add($"{symbol.Kind} **{symbol.Name}**");
        
        // Access level
        if (symbol.AccessLevel != "internal")
            lines.Add($"*{symbol.AccessLevel}*");

        // Modifiers
        if (symbol.IsStatic) lines.Add("*static*");
        if (symbol.IsAbstract) lines.Add("*abstract*");
        if (symbol.IsVirtual) lines.Add("*virtual*");

        // Signature
        if (!string.IsNullOrEmpty(symbol.Signature))
            lines.Add($"```\n{symbol.Signature}\n```");

        // Base types
        if (symbol.BaseTypes.Count > 0)
            lines.Add($"**Extends:** {string.Join(", ", symbol.BaseTypes)}");

        // Interfaces
        if (symbol.ImplementedInterfaces.Count > 0)
            lines.Add($"**Implements:** {string.Join(", ", symbol.ImplementedInterfaces)}");

        // Documentation
        if (!string.IsNullOrEmpty(symbol.Documentation))
            lines.Add($"\n{symbol.Documentation}");

        return string.Join("\n", lines);
    }

    private string ExtractTypeName(string fullyQualifiedName)
    {
        var parts = fullyQualifiedName.Split('.');
        return parts[^1]; // Last part
    }
}

public sealed class TypeHierarchy
{
    public LsifSymbol Type { get; set; } = new();
    public List<LsifSymbol> BaseTypes { get; set; } = new();
    public List<LsifSymbol> DerivedTypes { get; set; } = new();
    public List<LsifSymbol> ImplementingTypes { get; set; } = new();
}

public sealed class HoverInformation
{
    public string Content { get; set; } = "";
    public string? DocumentationUrl { get; set; }
}

public sealed class SymbolSearchCriteria
{
    public string? NamePattern { get; set; }
    public string? Kind { get; set; }
    public string? AccessLevel { get; set; }
    public bool? IsStatic { get; set; }
    public bool? IsAbstract { get; set; }
}
