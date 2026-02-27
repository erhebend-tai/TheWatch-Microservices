using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace TheWatch.Shared.Security;

/// <summary>
/// Serilog log event enricher that detects and masks Personally Identifiable Information (PII)
/// in log event properties. Applied globally via <c>.Enrich.With&lt;PiiMaskingEnricher&gt;()</c>.
/// </summary>
/// <remarks>
/// Masking rules (NIST SP 800-122 / CMMC Level 2):
/// <list type="bullet">
///   <item>Email: <c>u***@domain.com</c> (domain retained for forensic source tracing)</item>
///   <item>Phone: <c>***-**-1234</c> (last 4 digits retained)</item>
///   <item>IPv4: first two octets retained, rest masked: <c>192.168.xxx.xxx</c></item>
///   <item>SSN: fully masked: <c>***-**-****</c></item>
///   <item>GPS: rounded to 2 decimal places (city-level precision)</item>
/// </list>
/// </remarks>
public partial class PiiMaskingEnricher : ILogEventEnricher
{
    // Pre-compiled regex patterns for PII detection

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\d{3}[\-\.\s]?\d{3}[\-\.\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b", RegexOptions.Compiled)]
    private static partial Regex IpV4Pattern();

    [GeneratedRegex(@"\b\d{3}[\-\s]?\d{2}[\-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    [GeneratedRegex(@"[\-]?\d{1,3}\.\d{3,}", RegexOptions.Compiled)]
    private static partial Regex GpsCoordinatePattern();

    /// <summary>
    /// Enriches a log event by scanning all scalar string properties for PII patterns
    /// and replacing matching values with masked equivalents.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToUpdate = new List<LogEventProperty>();

        foreach (var property in logEvent.Properties)
        {
            if (property.Value is ScalarValue { Value: string stringValue })
            {
                var masked = MaskPii(stringValue);
                if (!string.Equals(masked, stringValue, StringComparison.Ordinal))
                {
                    propertiesToUpdate.Add(
                        propertyFactory.CreateProperty(property.Key, masked));
                }
            }
            else if (property.Value is StructureValue structureValue)
            {
                var updatedProperties = MaskStructure(structureValue, propertyFactory);
                if (updatedProperties is not null)
                {
                    propertiesToUpdate.Add(
                        new LogEventProperty(property.Key,
                            new StructureValue(updatedProperties, structureValue.TypeTag)));
                }
            }
        }

        foreach (var prop in propertiesToUpdate)
        {
            logEvent.AddOrUpdateProperty(prop);
        }
    }

    /// <summary>
    /// Scans the properties of a structured log value and masks any string values
    /// containing PII. Returns null if no masking was needed.
    /// </summary>
    private static List<LogEventProperty>? MaskStructure(
        StructureValue structureValue, ILogEventPropertyFactory propertyFactory)
    {
        var anyChanged = false;
        var updatedProperties = new List<LogEventProperty>(structureValue.Properties.Count);

        foreach (var prop in structureValue.Properties)
        {
            if (prop.Value is ScalarValue { Value: string stringValue })
            {
                var masked = MaskPii(stringValue);
                if (!string.Equals(masked, stringValue, StringComparison.Ordinal))
                {
                    updatedProperties.Add(
                        new LogEventProperty(prop.Name, new ScalarValue(masked)));
                    anyChanged = true;
                    continue;
                }
            }

            updatedProperties.Add(new LogEventProperty(prop.Name, prop.Value));
        }

        return anyChanged ? updatedProperties : null;
    }

    /// <summary>
    /// Applies all PII masking rules to the input string.
    /// Order matters: SSN before phone (SSN regex is a subset of phone).
    /// </summary>
    internal static string MaskPii(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // SSN: full mask (must run before phone — SSN pattern overlaps)
        input = SsnPattern().Replace(input, "***-**-****");

        // Email: keep domain, mask local part
        input = EmailPattern().Replace(input, match =>
        {
            var atIndex = match.Value.IndexOf('@');
            if (atIndex <= 0) return match.Value;
            var firstChar = match.Value[0];
            var domain = match.Value[(atIndex + 1)..];
            return $"{firstChar}***@{domain}";
        });

        // Phone: keep last 4 digits
        input = PhonePattern().Replace(input, match =>
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            if (digits.Length < 4) return "***";
            return $"***-**-{digits[^4..]}";
        });

        // IPv4: keep first two octets
        input = IpV4Pattern().Replace(input, match =>
        {
            var octet1 = match.Groups[1].Value;
            var octet2 = match.Groups[2].Value;
            return $"{octet1}.{octet2}.xxx.xxx";
        });

        // GPS coordinates: round to 2 decimal places (city-level precision)
        input = GpsCoordinatePattern().Replace(input, match =>
        {
            if (double.TryParse(match.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var coordinate))
            {
                return Math.Round(coordinate, 2)
                    .ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            }

            return match.Value;
        });

        return input;
    }
}
