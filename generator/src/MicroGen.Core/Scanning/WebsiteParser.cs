using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Parses HTML files and URLs into <see cref="ServiceDescriptor"/> objects.
/// Extracts forms, inputs, navigation structure, and detects Swagger UI pages.
/// </summary>
public sealed class WebsiteParser : ISourceParser
{
    private readonly ILogger<WebsiteParser> _logger;
    private readonly SpecParser? _specParser;

    public WebsiteParser(ILogger<WebsiteParser> logger, SpecParser? specParser = null)
    {
        _logger = logger;
        _specParser = specParser;
    }

    public IReadOnlyList<string> SupportedExtensions => [".html", ".htm"];

    public bool CanParse(string pathOrUri)
    {
        if (pathOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        var ext = Path.GetExtension(pathOrUri);
        return SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ServiceDescriptor> ParseAsync(string pathOrUri, string domainName, CancellationToken ct = default)
    {
        _logger.LogDebug("Parsing website/HTML source {Source}", pathOrUri);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var isUrl = pathOrUri.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        IDocument document;

        if (isUrl)
        {
            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            document = await context.OpenAsync(pathOrUri, ct).ConfigureAwait(false);

            // Check for Swagger UI — delegate to SpecParser if found
            var swaggerSpecUrl = DetectSwaggerSpec(document, pathOrUri);
            if (swaggerSpecUrl is not null && _specParser is not null)
            {
                _logger.LogInformation("Detected Swagger UI at {Url}, delegating to SpecParser with spec {Spec}",
                    pathOrUri, swaggerSpecUrl);

                // Download the spec to a temp file and delegate
                using var http = new HttpClient();
                var specContent = await http.GetStringAsync(swaggerSpecUrl, ct).ConfigureAwait(false);
                var tempFile = Path.Combine(Path.GetTempPath(), $"swagger_{Guid.NewGuid()}.json");
                try
                {
                    await File.WriteAllTextAsync(tempFile, specContent, ct).ConfigureAwait(false);
                    return await _specParser.ParseAsync(tempFile, domainName, ct).ConfigureAwait(false);
                }
                finally
                {
                    try { File.Delete(tempFile); } catch { /* best effort */ }
                }
            }
        }
        else
        {
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var htmlContent = await File.ReadAllTextAsync(pathOrUri, ct).ConfigureAwait(false);
            document = await context.OpenAsync(req => req.Content(htmlContent), ct).ConfigureAwait(false);
        }

        var result = ExtractFromHtml(document, pathOrUri, domainName);

        sw.Stop();
        _logger.LogInformation(
            "Parsed HTML {Source} in {Elapsed}ms: {Ops} operations, {Schemas} schemas",
            isUrl ? pathOrUri : Path.GetFileName(pathOrUri),
            sw.ElapsedMilliseconds, result.Operations.Count, result.Schemas.Count);

        return result;
    }

    private ServiceDescriptor ExtractFromHtml(IDocument document, string source, string domainName)
    {
        var serviceName = ExtractServiceName(document, source);
        var title = document.QuerySelector("title")?.TextContent?.Trim() ?? serviceName;
        var description = document.QuerySelector("meta[name='description']")
            ?.GetAttribute("content") ?? string.Empty;

        var schemas = new List<SchemaDescriptor>();
        var operations = new List<OperationDescriptor>();
        var tags = new List<TagDescriptor>();

        // Extract navigation links → tags
        var navLinks = document.QuerySelectorAll("nav a[href]");
        foreach (var link in navLinks)
        {
            var text = link.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text.Length > 1 && text.Length < 50)
            {
                var tagName = NamingHelper.ToPascalCase(text);
                if (!tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(new TagDescriptor
                    {
                        Name = tagName,
                        Description = link.GetAttribute("href") ?? string.Empty
                    });
                }
            }
        }

        // Extract forms → operations + schemas
        var forms = document.QuerySelectorAll("form");
        foreach (var form in forms)
        {
            var (op, formSchemas) = ExtractFormOperation(form, tags);
            if (op is not null)
            {
                operations.Add(op);
                schemas.AddRange(formSchemas);
            }
        }

        return new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = source,
            Title = title,
            Description = description,
            Tags = tags.Count > 0 ? tags : [new TagDescriptor { Name = "Default" }],
            Operations = operations,
            Schemas = schemas,
            SecuritySchemes = document.QuerySelector("form input[type='password']") is not null
                ? ["BearerAuth"] : []
        };
    }

    private static (OperationDescriptor? Operation, List<SchemaDescriptor> Schemas) ExtractFormOperation(
        IElement form, List<TagDescriptor> existingTags)
    {
        var action = form.GetAttribute("action") ?? "/";
        var method = (form.GetAttribute("method") ?? "POST").ToUpperInvariant();
        var formName = form.GetAttribute("name") ?? form.GetAttribute("id") ?? ExtractFormName(action);
        var pascalName = NamingHelper.ToPascalCase(formName);
        var schemas = new List<SchemaDescriptor>();

        // Collect input fields → properties
        var properties = new List<Models.PropertyDescriptor>();
        var enumSchemas = new List<SchemaDescriptor>();

        var inputs = form.QuerySelectorAll("input, textarea, select");
        foreach (var input in inputs)
        {
            var name = input.GetAttribute("name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (input.GetAttribute("type") is "hidden" or "submit" or "button") continue;

            var inputType = input.GetAttribute("type") ?? "text";

            // Select elements → enum schemas
            if (input is IHtmlSelectElement selectEl)
            {
                var options = selectEl.Options
                    .Select(o => o.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                if (options.Count > 0)
                {
                    var enumName = $"{pascalName}{NamingHelper.ToPascalCase(name)}";
                    enumSchemas.Add(new SchemaDescriptor
                    {
                        Name = enumName,
                        Type = "string",
                        IsEnum = true,
                        EnumValues = options
                    });
                }
            }

            var (type, format) = MapHtmlInputType(inputType);
            var prop = new Models.PropertyDescriptor
            {
                Name = NamingHelper.ToCamelCase(name),
                Type = type,
                Format = format,
                Description = ExtractInputLabel(input, form),
                Required = input.HasAttribute("required"),
                MaxLength = int.TryParse(input.GetAttribute("maxlength"), out var ml) ? ml : null,
                Minimum = double.TryParse(input.GetAttribute("min"), out var min) ? min : null,
                Maximum = double.TryParse(input.GetAttribute("max"), out var max) ? max : null,
                Pattern = input.GetAttribute("pattern")
            };
            properties.Add(prop);
        }

        if (properties.Count == 0) return (null, []);

        // Build request body schema
        var requestSchema = new SchemaDescriptor
        {
            Name = $"{pascalName}Request",
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };
        schemas.Add(requestSchema);
        schemas.AddRange(enumSchemas);

        var tag = existingTags.FirstOrDefault()?.Name ?? "Default";

        var op = new OperationDescriptor
        {
            OperationId = NamingHelper.ToCamelCase(formName),
            HttpMethod = method == "GET" ? "GET" : "POST",
            Path = NormalizePath(action),
            Summary = $"Submit {formName} form",
            Tag = tag,
            RequestBody = requestSchema,
            Responses = new Dictionary<string, ResponseDescriptor>
            {
                ["200"] = new() { StatusCode = "200", Description = "Form submitted successfully" },
                ["400"] = new() { StatusCode = "400", Description = "Validation error" }
            }
        };

        return (op, schemas);
    }

    private static (string Type, string? Format) MapHtmlInputType(string inputType) => inputType.ToLowerInvariant() switch
    {
        "email" => ("string", "email"),
        "url" => ("string", "uri"),
        "number" or "range" => ("number", "double"),
        "date" => ("string", "date"),
        "datetime-local" or "datetime" => ("string", "date-time"),
        "time" => ("string", "time"),
        "checkbox" => ("boolean", null),
        "file" => ("string", "binary"),
        "tel" => ("string", "phone"),
        "color" => ("string", null),
        _ => ("string", null) // text, password, search, etc.
    };

    private static string? DetectSwaggerSpec(IDocument document, string baseUrl)
    {
        // Look for swagger-initializer.js reference
        var scripts = document.QuerySelectorAll("script[src]");
        if (scripts.Any(s => s.GetAttribute("src")?.Contains("swagger", StringComparison.OrdinalIgnoreCase) == true))
        {
            // Try common spec paths
            var uri = new Uri(baseUrl);
            var candidates = new[]
            {
                "/swagger.json", "/swagger/v1/swagger.json",
                "/openapi.json", "/v1/openapi.json",
                "/api-docs", "/v2/api-docs"
            };

            foreach (var candidate in candidates)
            {
                // Check if there's a link to the spec in the page
                var links = document.QuerySelectorAll($"a[href*='{candidate}']");
                if (links.Length > 0)
                    return new Uri(uri, candidate).ToString();
            }

            // Default to /swagger.json
            return new Uri(uri, "/swagger.json").ToString();
        }

        // Check for inline spec URL in SwaggerUIBundle config
        var inlineScripts = document.QuerySelectorAll("script:not([src])");
        foreach (var script in inlineScripts)
        {
            var text = script.TextContent;
            if (text.Contains("SwaggerUIBundle", StringComparison.OrdinalIgnoreCase))
            {
                // Look for url: "..." pattern
                var urlMatch = System.Text.RegularExpressions.Regex.Match(text, @"url\s*:\s*[""']([^""']+)[""']");
                if (urlMatch.Success)
                {
                    var specUrl = urlMatch.Groups[1].Value;
                    if (!specUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        specUrl = new Uri(new Uri(baseUrl), specUrl).ToString();
                    return specUrl;
                }
            }
        }

        return null;
    }

    private static string ExtractServiceName(IDocument document, string source)
    {
        // Try title first
        var title = document.QuerySelector("title")?.TextContent?.Trim();
        if (!string.IsNullOrWhiteSpace(title) && title.Length < 60)
        {
            // Clean up common suffixes
            var cleaned = title
                .Replace(" - Home", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" | Home", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (cleaned.Length > 0 && cleaned.Length < 40)
                return NamingHelper.ToPascalCase(cleaned);
        }

        // Fall back to filename or hostname
        if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(source);
            return NamingHelper.ToPascalCase(uri.Host.Split('.')[0]);
        }

        return NamingHelper.ServiceNameFromFile(source);
    }

    private static string ExtractFormName(string action)
    {
        if (string.IsNullOrWhiteSpace(action) || action == "/" || action == "#")
            return "Form";

        var segments = action.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : "Form";
    }

    private static string ExtractInputLabel(IElement input, IElement form)
    {
        // Try associated label
        var id = input.GetAttribute("id");
        if (id is not null)
        {
            var label = form.QuerySelector($"label[for='{id}']");
            if (label is not null)
                return label.TextContent.Trim();
        }

        // Try placeholder
        var placeholder = input.GetAttribute("placeholder");
        if (!string.IsNullOrWhiteSpace(placeholder))
            return placeholder;

        // Try aria-label
        var ariaLabel = input.GetAttribute("aria-label");
        if (!string.IsNullOrWhiteSpace(ariaLabel))
            return ariaLabel;

        return string.Empty;
    }

    private static string NormalizePath(string action)
    {
        if (string.IsNullOrWhiteSpace(action) || action == "#")
            return "/";

        // Strip protocol+host if present
        if (action.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(action);
            return uri.AbsolutePath;
        }

        return action.StartsWith('/') ? action : $"/{action}";
    }
}
