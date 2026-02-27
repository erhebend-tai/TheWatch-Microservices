using System.Text.RegularExpressions;

namespace MicroGen.Core.Models;

/// <summary>
/// Maps SQL Server data types to OpenAPI (type, format) pairs.
/// </summary>
public static partial class SqlTypeMapping
{
    /// <summary>
    /// Returns the OpenAPI (type, format) pair for a SQL Server column type.
    /// </summary>
    public static (string Type, string? Format) MapToOpenApi(string sqlType)
    {
        var normalized = NormalizeSqlType(sqlType);

        return normalized switch
        {
            "uniqueidentifier" => ("string", "uuid"),
            "int" => ("integer", "int32"),
            "bigint" => ("integer", "int64"),
            "smallint" => ("integer", "int32"),
            "tinyint" => ("integer", "int32"),
            "bit" => ("boolean", null),
            "float" => ("number", "double"),
            "real" => ("number", "float"),
            "decimal" or "numeric" or "money" or "smallmoney" => ("number", "decimal"),
            "nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext" => ("string", null),
            "datetime" or "datetime2" or "datetimeoffset" or "smalldatetime" => ("string", "date-time"),
            "date" => ("string", "date"),
            "time" => ("string", "time"),
            "varbinary" or "binary" or "image" => ("string", "binary"),
            "timestamp" or "rowversion" => ("string", "binary"),
            "xml" => ("string", null),
            "geography" or "geometry" => ("object", null),
            "hierarchyid" => ("string", null),
            "sql_variant" => ("string", null),
            _ => ("string", null)
        };
    }

    /// <summary>
    /// Extracts max length from a type definition like "NVARCHAR(100)".
    /// Returns null if MAX or no length specified.
    /// </summary>
    public static int? ExtractMaxLength(string sqlType)
    {
        var match = LengthRegex().Match(sqlType);
        if (!match.Success) return null;
        var val = match.Groups[1].Value;
        if (val.Equals("MAX", StringComparison.OrdinalIgnoreCase)) return null;
        return int.TryParse(val, out var len) ? len : null;
    }

    /// <summary>
    /// Extracts precision and scale from a type definition like "DECIMAL(10,2)".
    /// </summary>
    public static (int Precision, int Scale)? ExtractPrecisionScale(string sqlType)
    {
        var match = PrecisionScaleRegex().Match(sqlType);
        if (!match.Success) return null;
        if (int.TryParse(match.Groups[1].Value, out var p) &&
            int.TryParse(match.Groups[2].Value, out var s))
            return (p, s);
        return null;
    }

    /// <summary>
    /// Strips parenthesized arguments and normalizes to lowercase.
    /// "NVARCHAR(100)" -> "nvarchar", "DECIMAL(10,2)" -> "decimal"
    /// </summary>
    public static string NormalizeSqlType(string sqlType)
    {
        var idx = sqlType.IndexOf('(');
        var raw = idx >= 0 ? sqlType[..idx] : sqlType;
        return raw.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"\((\d+|MAX)\)", RegexOptions.IgnoreCase)]
    private static partial Regex LengthRegex();

    [GeneratedRegex(@"\((\d+)\s*,\s*(\d+)\)")]
    private static partial Regex PrecisionScaleRegex();
}
