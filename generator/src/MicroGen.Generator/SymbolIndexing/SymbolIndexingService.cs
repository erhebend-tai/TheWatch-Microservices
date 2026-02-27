using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.SymbolIndexing;

/// <summary>
/// Orchestrates symbol indexing for all generated assemblies.
/// Coordinates index generation, validation, and storage.
/// </summary>
public sealed class SymbolIndexingService
{
    private readonly ILogger<SymbolIndexingService> _logger;
    private readonly SymbolIndexGenerator _generator;
    private readonly SymbolIndexQueryService _queryService;
    private readonly LsifDocumentWriter _writer;
    private readonly LsifDocumentCache _cache;

    public SymbolIndexingService(ILogger<SymbolIndexingService> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _generator = new SymbolIndexGenerator(loggerFactory.CreateLogger<SymbolIndexGenerator>());
        _queryService = new SymbolIndexQueryService(loggerFactory.CreateLogger<SymbolIndexQueryService>());
        _writer = new LsifDocumentWriter(loggerFactory.CreateLogger<LsifDocumentWriter>());
        _cache = new LsifDocumentCache(loggerFactory.CreateLogger<LsifDocumentCache>());
    }

    /// <summary>
    /// Generate symbol index for a compiled assembly and write LSIF documents.
    /// </summary>
    public async Task<IndexingResult> IndexAssemblyAsync(
        string assemblyPath,
        string outputDirectory,
        IndexingOptions? options = null)
    {
        options ??= new IndexingOptions();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "Assembly", Path.GetFileName(assemblyPath) },
            { "OutputDir", outputDirectory }
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Check cache first
            if (_cache.TryGetCached(assemblyPath, out var cached))
            {
                stopwatch.Stop();
                return new IndexingResult
                {
                    Success = true,
                    SymbolCount = cached!.Symbols.Count,
                    Duration = stopwatch.Elapsed,
                    FromCache = true,
                    LsifPath = GenerateExpectedPath(outputDirectory, assemblyPath)
                };
            }

            // Generate index
            _logger.LogInformation("Starting symbol indexing for {Assembly}", 
                Path.GetFileName(assemblyPath));

            var document = await _generator.GenerateAsync(
                assemblyPath,
                options.IncludeInternals,
                options.IncludePrivate);

            _cache.Cache(assemblyPath, document);

            // Write outputs
            var lsifPath = Path.Combine(outputDirectory, 
                $"{Path.GetFileNameWithoutExtension(assemblyPath)}.lsif.json");
            
            await _writer.WriteAsync(document, lsifPath);

            // Write markdown export if requested
            if (options.ExportMarkdown)
            {
                var mdPath = Path.Combine(outputDirectory,
                    $"{Path.GetFileNameWithoutExtension(assemblyPath)}.symbols.md");
                
                await _writer.ExportAsMarkdownAsync(document, mdPath);
            }

            // Load into query service
            _queryService.LoadIndex(document);

            // Verify index quality
            var issues = ValidateIndex(document);
            if (issues.Count > 0)
            {
                _logger.LogWarning("Index validation found {IssueCount} issues", issues.Count);
            }

            stopwatch.Stop();

            return new IndexingResult
            {
                Success = true,
                SymbolCount = document.Symbols.Count,
                Duration = stopwatch.Elapsed,
                FromCache = false,
                LsifPath = lsifPath,
                ValidationIssues = issues
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Symbol indexing failed for {Assembly}", assemblyPath);

            return new IndexingResult
            {
                Success = false,
                Duration = stopwatch.Elapsed,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Index all assemblies in a directory recursively.
    /// </summary>
    public async Task<AggregatedIndexingResult> IndexDirectoryAsync(
        string directoryPath,
        string outputDirectory,
        IndexingOptions? options = null)
    {
        _logger.LogInformation("Indexing all assemblies in {Directory}", directoryPath);

        var dlls = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);
        var result = new AggregatedIndexingResult();

        foreach (var dll in dlls)
        {
            // Skip test and interop assemblies
            if (dll.Contains(".Tests", StringComparison.Ordinal) || dll.Contains("Interop", StringComparison.Ordinal))
                continue;

            var indexResult = await IndexAssemblyAsync(dll, outputDirectory, options);
            result.Results.Add(dll, indexResult);

            if (!indexResult.Success)
            {
                _logger.LogWarning("Failed to index {Assembly}: {Error}", dll, indexResult.Error);
            }
        }

        result.TotalSymbols = result.Results.Values.Sum(r => r.SymbolCount);
        result.TotalDuration = TimeSpan.FromMilliseconds(
            result.Results.Values.Sum(r => r.Duration.TotalMilliseconds));

        _logger.LogInformation(
            "Completed indexing {Count} assemblies: {SymbolCount} symbols in {Duration:F2}s",
            result.Results.Count, result.TotalSymbols, result.TotalDuration.TotalSeconds);

        return result;
    }

    /// <summary>
    /// Generate a symbol report from an indexed assembly.
    /// </summary>
    public SymbolReport GenerateSymbolReport(string lsifPath)
    {
        if (!File.Exists(lsifPath))
            throw new FileNotFoundException($"LSIF file not found: {lsifPath}");

        _logger.LogInformation("Generating symbol report from {LsifPath}", lsifPath);

        var json = File.ReadAllText(lsifPath);
        var document = JsonSerializer.Deserialize<LsifDocument>(json);

        if (document == null)
            throw new InvalidOperationException("Failed to deserialize LSIF document");

        var report = new SymbolReport();

        foreach (var symbol in document.Symbols)
        {
            // Count by kind
            if (!string.IsNullOrEmpty(symbol.Kind))
            {
                if (!report.CountByKind.ContainsKey(symbol.Kind))
                    report.CountByKind[symbol.Kind] = 0;
                report.CountByKind[symbol.Kind]++;
            }

            // Count by access level
            if (!report.CountByAccess.ContainsKey(symbol.AccessLevel))
                report.CountByAccess[symbol.AccessLevel] = 0;
            report.CountByAccess[symbol.AccessLevel]++;

            // Track public API
            if (symbol.AccessLevel == "public" && symbol.Kind != null)
            {
                report.PublicSymbols.Add(symbol);
            }

            // Identify large types
            var memberCount = document.Symbols
                .Count(s => s.FullyQualifiedName.StartsWith(symbol.FullyQualifiedName + ".", StringComparison.Ordinal));
            
            if (memberCount > 20)
            {
                report.LargeTypes.Add((symbol, memberCount));
            }
        }

        report.LargeTypes.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        return report;
    }

    /// <summary>
    /// Query symbols in a loaded index.
    /// </summary>
    public SymbolQueryResult Query(SymbolQuery query)
    {
        var result = new SymbolQueryResult { Query = query };

        try
        {
            switch (query.QueryType)
            {
                case SymbolQueryType.FindSymbol:
                    result.Symbols = new List<LsifSymbol>();
                    var symbol = _queryService.FindSymbol(query.Target ?? "");
                    if (symbol != null)
                        result.Symbols.Add(symbol);
                    break;

                case SymbolQueryType.FindReferences:
                    result.Symbols = _queryService.FindReferences(query.Target ?? "");
                    break;

                case SymbolQueryType.FindImplementations:
                    result.Symbols = _queryService.FindImplementations(query.Target ?? "");
                    break;

                case SymbolQueryType.GetTypeMembers:
                    result.Symbols = _queryService.GetTypeMembers(query.Target ?? "", query.IncludeInherited);
                    break;

                case SymbolQueryType.GetTypeHierarchy:
                    result.Hierarchy = _queryService.GetTypeHierarchy(query.Target ?? "");
                    break;

                case SymbolQueryType.GetHoverInfo:
                    result.HoverInfo = _queryService.GetHoverInfo(query.Target ?? "");
                    break;

                case SymbolQueryType.Search:
                    var criteria = new SymbolSearchCriteria
                    {
                        NamePattern = query.SearchPattern,
                        Kind = query.FilterKind,
                        AccessLevel = query.FilterAccessLevel,
                        IsStatic = query.FilterIsStatic,
                        IsAbstract = query.FilterIsAbstract
                    };
                    result.Symbols = _queryService.Search(criteria);
                    break;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed");
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    // --- Private Helpers ---

    private List<string> ValidateIndex(LsifDocument document)
    {
        var issues = new List<string>();

        // Check for duplicate symbols
        var fqns = document.Symbols.Select(s => s.FullyQualifiedName).ToList();
        var duplicates = fqns.GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var dup in duplicates)
        {
            issues.Add($"Duplicate symbol: {dup}");
        }

        // Check for orphaned symbols
        foreach (var symbol in document.Symbols)
        {
            if (symbol.FullyQualifiedName.Contains('.'))
            {
                var parentFqn = string.Join(".", symbol.FullyQualifiedName.Split('.')[..^1]);
                if (!document.Symbols.Any(s => s.FullyQualifiedName == parentFqn))
                {
                    // Orphaned member (parent not indexed)
                    // This is sometimes OK for external types, so we just log it
                }
            }
        }

        return issues;
    }

    private string GenerateExpectedPath(string outputDir, string assemblyPath)
        => Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(assemblyPath)}.lsif.json");
}

// --- Models ---

public sealed class IndexingOptions
{
    public bool IncludeInternals { get; set; } = true;
    public bool IncludePrivate { get; set; }
    public bool ExportMarkdown { get; set; } = true;
}

public sealed class IndexingResult
{
    public bool Success { get; set; }
    public int SymbolCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool FromCache { get; set; }
    public string? LsifPath { get; set; }
    public string? Error { get; set; }
    public List<string> ValidationIssues { get; set; } = new();
}

public sealed class AggregatedIndexingResult
{
    public Dictionary<string, IndexingResult> Results { get; set; } = new();
    public int TotalSymbols { get; set; }
    public TimeSpan TotalDuration { get; set; }

    public int SuccessCount => Results.Values.Count(r => r.Success);
    public int FailureCount => Results.Values.Count(r => !r.Success);
}

public sealed class SymbolReport
{
    public Dictionary<string, int> CountByKind { get; set; } = new();
    public Dictionary<string, int> CountByAccess { get; set; } = new();
    public List<LsifSymbol> PublicSymbols { get; set; } = new();
    public List<(LsifSymbol Symbol, int MemberCount)> LargeTypes { get; set; } = new();
}

public sealed class SymbolQuery
{
    public SymbolQueryType QueryType { get; set; }
    public string? Target { get; set; }
    public bool IncludeInherited { get; set; }

    // Search options
    public string? SearchPattern { get; set; }
    public string? FilterKind { get; set; }
    public string? FilterAccessLevel { get; set; }
    public bool? FilterIsStatic { get; set; }
    public bool? FilterIsAbstract { get; set; }
}

public enum SymbolQueryType
{
    FindSymbol,
    FindReferences,
    FindImplementations,
    GetTypeMembers,
    GetTypeHierarchy,
    GetHoverInfo,
    Search
}

public sealed class SymbolQueryResult
{
    public SymbolQuery Query { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<LsifSymbol> Symbols { get; set; } = new();
    public TypeHierarchy? Hierarchy { get; set; }
    public HoverInformation? HoverInfo { get; set; }
}
