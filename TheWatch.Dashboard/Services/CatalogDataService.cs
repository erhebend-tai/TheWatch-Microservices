using System.Text.Json;

namespace TheWatch.Dashboard.Services;

public class ApiSpec
{
    public string? Domain { get; set; }
    public string? FileName { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public int OperationCount { get; set; }
    public int SchemaCount { get; set; }
    public string? Relevance { get; set; }
    public List<ApiOperation> Operations { get; set; } = [];
}

public class ApiOperation
{
    public string? Domain { get; set; }
    public string? Spec { get; set; }
    public string? Method { get; set; }
    public string? Path { get; set; }
    public string? OperationId { get; set; }
    public string? Summary { get; set; }
    public string? Program { get; set; }
    public string? MatchStatus { get; set; }
    public List<string> Tags { get; set; } = [];
}

public class CatalogDataService
{
    private readonly IWebHostEnvironment _env;
    private List<ApiOperation>? _cachedOperations;

    public CatalogDataService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<List<ApiOperation>> GetAllOperationsAsync()
    {
        if (_cachedOperations is not null)
            return _cachedOperations;

        var filePath = Path.Combine(_env.WebRootPath, "data", "_catalog.json");
        if (!File.Exists(filePath))
        {
            // Try E: drive where catalog was generated
            filePath = @"E:\json_output\APIS\_catalog.json";
        }

        if (!File.Exists(filePath))
            return [];

        var json = await File.ReadAllTextAsync(filePath);
        var doc = JsonDocument.Parse(json);

        var operations = new List<ApiOperation>();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var spec in doc.RootElement.EnumerateArray())
            {
                var domain = spec.TryGetProperty("domain", out var d) ? d.GetString() : null;
                var specName = spec.TryGetProperty("fileName", out var fn) ? fn.GetString() : null;

                if (spec.TryGetProperty("operations", out var ops))
                {
                    foreach (var op in ops.EnumerateArray())
                    {
                        operations.Add(new ApiOperation
                        {
                            Domain = domain,
                            Spec = specName,
                            Method = op.TryGetProperty("method", out var m) ? m.GetString()?.ToUpper() : null,
                            Path = op.TryGetProperty("path", out var p) ? p.GetString() : null,
                            OperationId = op.TryGetProperty("operationId", out var oid) ? oid.GetString() : null,
                            Summary = op.TryGetProperty("summary", out var s) ? s.GetString() : null,
                            Tags = op.TryGetProperty("tags", out var t)
                                ? t.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                                : []
                        });
                    }
                }
            }
        }

        _cachedOperations = operations;
        return operations;
    }

    public async Task<int> GetTotalOperationCountAsync()
    {
        var ops = await GetAllOperationsAsync();
        return ops.Count;
    }
}
