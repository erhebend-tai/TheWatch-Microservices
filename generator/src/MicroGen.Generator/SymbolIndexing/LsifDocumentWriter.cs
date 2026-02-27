using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.SymbolIndexing;

/// <summary>
/// Writes LSIF documents to JSON files compliant with Language Server Index Format specification.
/// </summary>
public sealed class LsifDocumentWriter
{
    private readonly ILogger<LsifDocumentWriter> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public LsifDocumentWriter(ILogger<LsifDocumentWriter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Write an LSIF document to a JSON file.
    /// </summary>
    public async Task WriteAsync(LsifDocument document, string outputPath)
    {
        _logger.LogInformation("Writing LSIF document to {Path}", outputPath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var json = JsonSerializer.Serialize(document, JsonOptions);
            await File.WriteAllTextAsync(outputPath, json);
            
            var info = new FileInfo(outputPath);
            _logger.LogInformation(
                "LSIF document written successfully: {Size}KB, {SymbolCount} symbols",
                info.Length / 1024, document.Symbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write LSIF document: {Path}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Write a compact LSIF document with symbol tree structure.
    /// </summary>
    public async Task WriteCompactAsync(LsifDocument document, string outputPath)
    {
        _logger.LogInformation("Writing compact LSIF document to {Path}", outputPath);

        var compact = new CompactLsifDocument
        {
            Metadata = document.Metadata,
            SymbolTree = BuildSymbolTree(document.Symbols)
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var json = JsonSerializer.Serialize(compact, JsonOptions);
            await File.WriteAllTextAsync(outputPath, json);
            
            var info = new FileInfo(outputPath);
            _logger.LogInformation("Compact LSIF document written: {Size}KB", info.Length / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write compact LSIF document: {Path}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Export symbol index as a markdown file for documentation.
    /// </summary>
    public async Task ExportAsMarkdownAsync(LsifDocument document, string outputPath)
    {
        _logger.LogInformation("Exporting symbol index as markdown: {Path}", outputPath);

        var lines = new List<string>
        {
            "# Symbol Index",
            $"Generated: {document.Metadata.GeneratedAt:O}",
            $"Tool: {document.Metadata.ToolInfo.Name} v{document.Metadata.ToolInfo.Version}",
            "",
            $"## Summary",
            $"- Total Symbols: {document.Symbols.Count}",
            ""
        };

        // Group by kind
        var byKind = new Dictionary<string, List<LsifSymbol>>();
        foreach (var symbol in document.Symbols)
        {
            var kind = symbol.Kind ?? "unknown";
            if (!byKind.ContainsKey(kind))
                byKind[kind] = new List<LsifSymbol>();
            byKind[kind].Add(symbol);
        }

        foreach (var (kind, symbols) in byKind)
        {
            lines.Add($"## {kind.ToUpper(CultureInfo.InvariantCulture)} ({symbols.Count})");
            
            foreach (var symbol in symbols)
            {
                lines.Add($"### {symbol.FullyQualifiedName}");
                
                if (!string.IsNullOrEmpty(symbol.Documentation))
                {
                    lines.Add($"{symbol.Documentation}");
                }

                var meta = new List<string>();
                meta.Add($"- Access: `{symbol.AccessLevel}`");
                if (symbol.IsStatic) meta.Add("- Static");
                if (symbol.IsAbstract) meta.Add("- Abstract");
                if (symbol.IsVirtual) meta.Add("- Virtual");
                
                if (!string.IsNullOrEmpty(symbol.Signature))
                {
                    meta.Add($"- Signature: `{symbol.Signature}`");
                }

                if (symbol.BaseTypes.Count > 0)
                {
                    meta.Add($"- Extends: {string.Join(", ", symbol.BaseTypes)}");
                }

                if (symbol.ImplementedInterfaces.Count > 0)
                {
                    meta.Add($"- Implements: {string.Join(", ", symbol.ImplementedInterfaces)}");
                }

                lines.AddRange(meta);
                lines.Add("");
            }
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await File.WriteAllLinesAsync(outputPath, lines);
            _logger.LogInformation("Markdown export created: {Path}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export markdown: {Path}", outputPath);
            throw;
        }
    }

    private SymbolTreeNode BuildSymbolTree(List<LsifSymbol> symbols)
    {
        var root = new SymbolTreeNode { Name = "root" };
        var nodeMap = new Dictionary<string, SymbolTreeNode> { { "root", root } };

        // First pass: create all nodes
        foreach (var symbol in symbols)
        {
            nodeMap[symbol.FullyQualifiedName] = new SymbolTreeNode
            {
                Name = symbol.Name,
                FullyQualifiedName = symbol.FullyQualifiedName,
                Kind = symbol.Kind,
                AccessLevel = symbol.AccessLevel
            };
        }

        // Second pass: build parent-child relationships
        foreach (var symbol in symbols)
        {
            var parts = symbol.FullyQualifiedName.Split('.');
            if (parts.Length > 1)
            {
                var parentFqn = string.Join(".", parts[..^1]);
                if (nodeMap.TryGetValue(parentFqn, out var parent))
                {
                    var node = nodeMap[symbol.FullyQualifiedName];
                    parent.Children.Add(node);
                }
                else
                {
                    // Add to root if parent not found
                    root.Children.Add(nodeMap[symbol.FullyQualifiedName]);
                }
            }
            else
            {
                root.Children.Add(nodeMap[symbol.FullyQualifiedName]);
            }
        }

        return root;
    }
}

public sealed class CompactLsifDocument
{
    [JsonPropertyName("metadata")]
    public LsifMetadata Metadata { get; set; } = new();

    [JsonPropertyName("symbolTree")]
    public SymbolTreeNode SymbolTree { get; set; } = new();
}

public sealed class SymbolTreeNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("fullyQualifiedName")]
    public string? FullyQualifiedName { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("accessLevel")]
    public string? AccessLevel { get; set; }

    [JsonPropertyName("children")]
    public List<SymbolTreeNode> Children { get; set; } = new();
}

/// <summary>
/// Cache for generated LSIF documents to avoid regeneration.
/// </summary>
public sealed class LsifDocumentCache
{
    private readonly ILogger<LsifDocumentCache> _logger;
    private readonly Dictionary<string, CacheEntry> _cache = new();

    public LsifDocumentCache(ILogger<LsifDocumentCache> logger)
    {
        _logger = logger;
    }

    public bool TryGetCached(string assemblyPath, out LsifDocument? document)
    {
        if (_cache.TryGetValue(assemblyPath, out var entry))
        {
            var fileInfo = new FileInfo(assemblyPath);
            if (fileInfo.LastWriteTimeUtc == entry.AssemblyWriteTime)
            {
                _logger.LogDebug("Cache hit for {Assembly}", Path.GetFileName(assemblyPath));
                document = entry.Document;
                return true;
            }
            else
            {
                _logger.LogDebug("Cache invalidated for {Assembly} (file changed)", 
                    Path.GetFileName(assemblyPath));
                _cache.Remove(assemblyPath);
            }
        }

        document = null;
        return false;
    }

    public void Cache(string assemblyPath, LsifDocument document)
    {
        var fileInfo = new FileInfo(assemblyPath);
        _cache[assemblyPath] = new CacheEntry
        {
            Document = document,
            AssemblyWriteTime = fileInfo.LastWriteTimeUtc,
            CachedAt = DateTime.UtcNow
        };

        _logger.LogDebug("Cached LSIF for {Assembly}", Path.GetFileName(assemblyPath));
    }

    public void Clear()
    {
        _logger.LogInformation("Clearing LSIF document cache ({Count} entries)", _cache.Count);
        _cache.Clear();
    }

    private sealed class CacheEntry
    {
        public LsifDocument Document { get; set; } = new();
        public DateTime AssemblyWriteTime { get; set; }
        public DateTime CachedAt { get; set; }
    }
}
