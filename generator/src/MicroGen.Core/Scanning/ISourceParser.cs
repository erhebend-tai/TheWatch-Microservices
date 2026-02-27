using MicroGen.Core.Models;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Common interface for all source parsers (OpenAPI, SQL, CSV, GraphQL, HTML).
/// Each parser converts its input format into a <see cref="ServiceDescriptor"/>
/// so all downstream generators work unchanged.
/// </summary>
public interface ISourceParser
{
    /// <summary>File extensions this parser handles (e.g. ".yaml", ".sql").</summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>Returns true if this parser can handle the given file path or URI.</summary>
    bool CanParse(string pathOrUri);

    /// <summary>
    /// Parses a single source file or URI into a <see cref="ServiceDescriptor"/>.
    /// </summary>
    Task<ServiceDescriptor> ParseAsync(string pathOrUri, string domainName, CancellationToken ct = default);
}
