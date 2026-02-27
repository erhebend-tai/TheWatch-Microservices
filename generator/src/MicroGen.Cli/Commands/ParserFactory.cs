using MicroGen.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace MicroGen.Cli.Commands;

/// <summary>
/// Builds the list of <see cref="ISourceParser"/> instances based on the requested source type filter.
/// </summary>
internal static class ParserFactory
{
    /// <summary>
    /// Creates parser instances for the given source type filter.
    /// </summary>
    /// <param name="sourceType">
    /// Filter: "openapi", "sql", "csv", "graphql", "html", or "all" (default).
    /// </param>
    /// <param name="loggerFactory">Logger factory for parser construction.</param>
    public static List<ISourceParser> BuildParserList(string? sourceType, ILoggerFactory loggerFactory)
    {
        var filter = sourceType?.ToLowerInvariant() ?? "all";
        var parsers = new List<ISourceParser>();

        var specParser = new SpecParser(loggerFactory.CreateLogger<SpecParser>());

        if (filter is "all" or "openapi")
            parsers.Add(specParser);

        if (filter is "all" or "sql")
            parsers.Add(new SqlParser(loggerFactory.CreateLogger<SqlParser>()));

        if (filter is "all" or "csv")
            parsers.Add(new CsvParser(loggerFactory.CreateLogger<CsvParser>()));

        if (filter is "all" or "graphql")
            parsers.Add(new GraphQlParser(loggerFactory.CreateLogger<GraphQlParser>()));

        if (filter is "all" or "html")
            parsers.Add(new WebsiteParser(loggerFactory.CreateLogger<WebsiteParser>(), specParser));

        return parsers;
    }

    /// <summary>
    /// Returns the include patterns appropriate for a given source type.
    /// </summary>
    public static string[] GetIncludePatterns(string? sourceType) => (sourceType?.ToLowerInvariant() ?? "all") switch
    {
        "openapi" => ["*.yaml", "*.yml", "*.json"],
        "sql" => ["*.sql"],
        "csv" => ["*.csv"],
        "graphql" => ["*.gql", "*.graphql"],
        "html" => ["*.html", "*.htm"],
        _ => ["*.yaml", "*.yml", "*.json", "*.sql", "*.csv", "*.gql", "*.graphql"]
    };
}
