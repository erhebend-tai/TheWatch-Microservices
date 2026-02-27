using MicroGen.Core.Configuration;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Recursively scans directories for OpenAPI specification files
/// and builds a domain/service hierarchy.
/// </summary>
public sealed class SpecScanner
{
    private readonly ILogger<SpecScanner> _logger;
    private readonly SpecParser _parser;

    public SpecScanner(ILogger<SpecScanner> logger, SpecParser parser)
    {
        _logger = logger;
        _parser = parser;
    }

    /// <summary>
    /// Scans the given root path for OpenAPI specs and returns domain descriptors.
    /// </summary>
    public async Task<List<DomainDescriptor>> ScanAsync(
        string rootPath,
        GeneratorConfig config,
        CancellationToken cancellationToken = default)
    {
        var absoluteRoot = Path.GetFullPath(rootPath);
        if (!Directory.Exists(absoluteRoot))
            throw new DirectoryNotFoundException($"Scan root not found: {absoluteRoot}");

        _logger.LogInformation("Scanning {Root} for OpenAPI specifications...", absoluteRoot);

        var specFiles = DiscoverSpecFiles(absoluteRoot, config);
        _logger.LogInformation("Found {Count} specification files", specFiles.Count);

        var domainMap = new Dictionary<string, DomainDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var specFile in specFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var domainName = DeriveDomainName(absoluteRoot, specFile);
                var service = await _parser.ParseAsync(specFile, domainName, cancellationToken);

                if (!domainMap.TryGetValue(domainName, out var domain))
                {
                    domain = new DomainDescriptor
                    {
                        Name = domainName,
                        DirectoryPath = Path.GetDirectoryName(specFile) ?? absoluteRoot
                    };
                    domainMap[domainName] = domain;
                }

                domain.Services.Add(service);
                _logger.LogInformation(
                    "  [{Domain}] {Service}: {Ops} operations, {Schemas} schemas",
                    domainName, service.ServiceName, service.TotalOperations, service.Schemas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse {File}, skipping", specFile);
            }
        }

        var domains = domainMap.Values
            .OrderBy(d => d.Name)
            .ToList();

        _logger.LogInformation(
            "Scan complete: {Domains} domains, {Services} services, {Ops} total operations",
            domains.Count,
            domains.Sum(d => d.Services.Count),
            domains.Sum(d => d.TotalOperations));

        return domains;
    }

    private List<string> DiscoverSpecFiles(string root, GeneratorConfig config)
    {
        var searchOption = config.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = new List<string>();

        foreach (var pattern in config.IncludePatterns)
        {
            files.AddRange(Directory.EnumerateFiles(root, pattern, searchOption));
        }

        // Apply exclusions
        var excluded = config.ExcludePatterns
            .SelectMany(p =>
            {
                try { return Directory.EnumerateFiles(root, p, searchOption); }
                catch { return []; }
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Also exclude files in _shared, _testing, node_modules directories
        files = files
            .Where(f => !excluded.Contains(f))
            .Where(f => !IsExcludedDirectory(root, f, config.ExcludePatterns))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

        return files;
    }

    private static bool IsExcludedDirectory(string root, string file, string[] patterns)
    {
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
        var excludeDirs = new[] { "_shared/", "_testing/", "node_modules/", ".github/", "generator/", "dist/", "cloud/", "external/", "resumes/", "scripts/", "docs/", "_Data/", ".claude/", ".idea/" };
        return excludeDirs.Any(d => relative.StartsWith(d, StringComparison.OrdinalIgnoreCase));
    }

    private static string DeriveDomainName(string root, string specFile)
    {
        var relative = Path.GetRelativePath(root, specFile);
        var dir = Path.GetDirectoryName(relative);

        if (string.IsNullOrEmpty(dir) || dir == ".")
            return "default";

        // Take the first directory segment as domain
        var firstSegment = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return NamingHelper.ToPascalCase(firstSegment);
    }
}
