using MicroGen.Core.Configuration;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Unified scanner that routes source files to the appropriate <see cref="ISourceParser"/>
/// based on file extension. Replaces tight SpecScanner↔SpecParser coupling when
/// scanning multi-format inputs.
/// </summary>
public sealed class SourceScanner
{
    private readonly ILogger<SourceScanner> _logger;
    private readonly Dictionary<string, ISourceParser> _parsersByExtension;
    private readonly List<ISourceParser> _parsers;

    public SourceScanner(ILogger<SourceScanner> logger, IEnumerable<ISourceParser> parsers)
    {
        _logger = logger;
        _parsers = parsers.ToList();
        _parsersByExtension = new Dictionary<string, ISourceParser>(StringComparer.OrdinalIgnoreCase);

        foreach (var parser in _parsers)
        {
            foreach (var ext in parser.SupportedExtensions)
            {
                _parsersByExtension.TryAdd(ext, parser);
            }
        }

        _logger.LogDebug("SourceScanner initialized with {Count} parsers, {ExtCount} extensions",
            _parsers.Count, _parsersByExtension.Count);
    }

    /// <summary>
    /// Scans the given root path for all supported source files and returns domain descriptors.
    /// </summary>
    public async Task<List<DomainDescriptor>> ScanAsync(
        string rootPath,
        GeneratorConfig config,
        CancellationToken cancellationToken = default)
    {
        var absoluteRoot = Path.GetFullPath(rootPath);
        if (!Directory.Exists(absoluteRoot))
            throw new DirectoryNotFoundException($"Scan root not found: {absoluteRoot}");

        _logger.LogInformation("Scanning {Root} for source files ({Extensions})...",
            absoluteRoot, string.Join(", ", config.IncludePatterns));

        var sourceFiles = DiscoverSourceFiles(absoluteRoot, config);
        _logger.LogInformation("Found {Count} source files", sourceFiles.Count);

        var domainMap = new Dictionary<string, DomainDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(sourceFile);
            if (!_parsersByExtension.TryGetValue(ext, out var parser))
            {
                // Try CanParse fallback
                parser = _parsers.FirstOrDefault(p => p.CanParse(sourceFile));
                if (parser is null)
                {
                    _logger.LogDebug("No parser found for {File}, skipping", sourceFile);
                    continue;
                }
            }

            try
            {
                var domainName = DeriveDomainName(absoluteRoot, sourceFile);
                var service = await parser.ParseAsync(sourceFile, domainName, cancellationToken);

                if (!domainMap.TryGetValue(domainName, out var domain))
                {
                    domain = new DomainDescriptor
                    {
                        Name = domainName,
                        DirectoryPath = Path.GetDirectoryName(sourceFile) ?? absoluteRoot
                    };
                    domainMap[domainName] = domain;
                }

                // Check if service with same name already exists (merge)
                var existing = domain.Services.FirstOrDefault(s =>
                    s.ServiceName.Equals(service.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    var merged = MergeServices(existing, service);
                    domain.Services.Remove(existing);
                    domain.Services.Add(merged);
                    _logger.LogInformation(
                        "  [{Domain}] Merged {Service} from {File}: {Ops} operations, {Schemas} schemas",
                        domainName, service.ServiceName, Path.GetFileName(sourceFile),
                        merged.Operations.Count, merged.Schemas.Count);
                }
                else
                {
                    domain.Services.Add(service);
                    _logger.LogInformation(
                        "  [{Domain}] {Service} ({Parser}): {Ops} operations, {Schemas} schemas",
                        domainName, service.ServiceName, parser.GetType().Name,
                        service.Operations.Count, service.Schemas.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse {File}, skipping", sourceFile);
            }
        }

        // Also process URL sources
        if (config.UrlSources.Length > 0)
        {
            var webParser = _parsers.OfType<WebsiteParser>().FirstOrDefault();
            if (webParser is not null)
            {
                foreach (var url in config.UrlSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var domainName = "Web";
                        var service = await webParser.ParseAsync(url, domainName, cancellationToken);

                        if (!domainMap.TryGetValue(domainName, out var domain))
                        {
                            domain = new DomainDescriptor
                            {
                                Name = domainName,
                                DirectoryPath = absoluteRoot
                            };
                            domainMap[domainName] = domain;
                        }

                        domain.Services.Add(service);
                        _logger.LogInformation(
                            "  [{Domain}] {Service} (URL): {Ops} operations, {Schemas} schemas",
                            domainName, service.ServiceName, service.Operations.Count, service.Schemas.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse URL {Url}, skipping", url);
                    }
                }
            }
        }

        var domains = domainMap.Values.OrderBy(d => d.Name).ToList();

        _logger.LogInformation(
            "Scan complete: {Domains} domains, {Services} services, {Ops} total operations",
            domains.Count,
            domains.Sum(d => d.Services.Count),
            domains.Sum(d => d.TotalOperations));

        return domains;
    }

    /// <summary>
    /// Scans for URL sources only (no file system).
    /// </summary>
    public async Task<List<DomainDescriptor>> ScanUrlsAsync(
        string[] urls,
        CancellationToken cancellationToken = default)
    {
        var webParser = _parsers.OfType<WebsiteParser>().FirstOrDefault();
        if (webParser is null)
        {
            _logger.LogWarning("No WebsiteParser available for URL scanning");
            return [];
        }

        var domainMap = new Dictionary<string, DomainDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var domainName = "Web";
                var service = await webParser.ParseAsync(url, domainName, cancellationToken);

                if (!domainMap.TryGetValue(domainName, out var domain))
                {
                    domain = new DomainDescriptor
                    {
                        Name = domainName,
                        DirectoryPath = Directory.GetCurrentDirectory()
                    };
                    domainMap[domainName] = domain;
                }

                domain.Services.Add(service);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse URL {Url}", url);
            }
        }

        return domainMap.Values.OrderBy(d => d.Name).ToList();
    }

    private List<string> DiscoverSourceFiles(string root, GeneratorConfig config)
    {
        var searchOption = config.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = new List<string>();

        foreach (var pattern in config.IncludePatterns)
        {
            try
            {
                files.AddRange(Directory.EnumerateFiles(root, pattern, searchOption));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error searching for pattern {Pattern}", pattern);
            }
        }

        // Apply exclusions
        var excluded = config.ExcludePatterns
            .SelectMany(p =>
            {
                try { return Directory.EnumerateFiles(root, p, searchOption); }
                catch { return Enumerable.Empty<string>(); }
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        files = files
            .Where(f => !excluded.Contains(f))
            .Where(f => !IsExcludedDirectory(root, f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

        return files;
    }

    private static bool IsExcludedDirectory(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
        var excludeDirs = new[]
        {
            "_shared/", "_testing/", "node_modules/", ".github/", "generator/",
            "dist/", "cloud/", "external/", "resumes/", "scripts/", "docs/",
            "_Data/", ".claude/", ".idea/", "bin/", "obj/", "generated/",
            ".git/", "packages/", "csharp-interfaces/"
        };
        return excludeDirs.Any(d => relative.StartsWith(d, StringComparison.OrdinalIgnoreCase));
    }

    private static string DeriveDomainName(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var dir = Path.GetDirectoryName(relative);

        if (string.IsNullOrEmpty(dir) || dir == ".")
            return "Default";

        var firstSegment = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return NamingHelper.ToPascalCase(firstSegment);
    }

    private static ServiceDescriptor MergeServices(ServiceDescriptor existing, ServiceDescriptor additional)
    {
        // Merge operations (deduplicate by operationId)
        var mergedOps = new List<OperationDescriptor>(existing.Operations);
        var existingOpIds = new HashSet<string>(existing.Operations.Select(o => o.OperationId),
            StringComparer.OrdinalIgnoreCase);
        foreach (var op in additional.Operations)
        {
            if (!existingOpIds.Contains(op.OperationId))
                mergedOps.Add(op);
        }

        // Merge schemas (deduplicate by name)
        var mergedSchemas = new List<SchemaDescriptor>(existing.Schemas);
        var existingSchemaNames = new HashSet<string>(existing.Schemas.Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);
        foreach (var schema in additional.Schemas)
        {
            if (!existingSchemaNames.Contains(schema.Name))
                mergedSchemas.Add(schema);
        }

        // Merge tags (deduplicate by name)
        var mergedTags = new List<TagDescriptor>(existing.Tags);
        var existingTagNames = new HashSet<string>(existing.Tags.Select(t => t.Name),
            StringComparer.OrdinalIgnoreCase);
        foreach (var tag in additional.Tags)
        {
            if (!existingTagNames.Contains(tag.Name))
                mergedTags.Add(tag);
        }

        // Merge security schemes
        var mergedSecurity = existing.SecuritySchemes
            .Union(additional.SecuritySchemes, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return existing with
        {
            Operations = mergedOps,
            Schemas = mergedSchemas,
            Tags = mergedTags,
            SecuritySchemes = mergedSecurity
        };
    }
}
