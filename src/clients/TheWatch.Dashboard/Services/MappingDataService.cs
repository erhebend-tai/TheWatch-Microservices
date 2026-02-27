using System.Text.Json;

namespace TheWatch.Dashboard.Services;

public class MappingEntry
{
    public string? Program { get; set; }
    public string? Domain { get; set; }
    public string? Method { get; set; }
    public string? Path { get; set; }
    public string? OperationId { get; set; }
    public string? Summary { get; set; }
    public string? MatchStatus { get; set; }
    public string? SourceSpec { get; set; }
}

public class ModelEntry
{
    public string? Name { get; set; }
    public string? Kind { get; set; }
    public int PropertyCount { get; set; }
    public string? MatchType { get; set; }
    public string? Program { get; set; }
    public string? SchemaSource { get; set; }
}

public class MappingData
{
    public List<MappingEntry> Operations { get; set; } = [];
    public List<ModelEntry> Models { get; set; } = [];
    public Dictionary<string, ProgramStats> ProgramStats { get; set; } = [];
}

public class ProgramStats
{
    public int MatchedOperations { get; set; }
    public int UnmatchedOperations { get; set; }
    public int MatchedModels { get; set; }
    public int UnmatchedModels { get; set; }
    public int TotalOperations => MatchedOperations + UnmatchedOperations;
    public int TotalModels => MatchedModels + UnmatchedModels;
    public double OperationCoverage => TotalOperations > 0 ? (double)MatchedOperations / TotalOperations * 100 : 0;
}

public class MappingDataService
{
    private readonly IWebHostEnvironment _env;
    private MappingData? _cachedData;

    public MappingDataService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<MappingData> GetMappingDataAsync()
    {
        if (_cachedData is not null)
            return _cachedData;

        var filePath = Path.Combine(_env.WebRootPath, "data", "_mapping.json");
        if (!File.Exists(filePath))
        {
            // Try the solution root
            filePath = Path.Combine(_env.ContentRootPath, "..", "_mapping.json");
        }

        if (!File.Exists(filePath))
            return new MappingData();

        var json = await File.ReadAllTextAsync(filePath);
        var doc = JsonDocument.Parse(json);

        var data = new MappingData();

        // Parse operations from mapping structure
        if (doc.RootElement.TryGetProperty("operations", out var opsElement))
        {
            foreach (var op in opsElement.EnumerateArray())
            {
                data.Operations.Add(new MappingEntry
                {
                    Program = op.TryGetProperty("program", out var p) ? p.GetString() : null,
                    Domain = op.TryGetProperty("domain", out var d) ? d.GetString() : null,
                    Method = op.TryGetProperty("method", out var m) ? m.GetString() : null,
                    Path = op.TryGetProperty("path", out var path) ? path.GetString() : null,
                    OperationId = op.TryGetProperty("operationId", out var oid) ? oid.GetString() : null,
                    Summary = op.TryGetProperty("summary", out var s) ? s.GetString() : null,
                    MatchStatus = op.TryGetProperty("matchStatus", out var ms) ? ms.GetString() : "Unmatched",
                    SourceSpec = op.TryGetProperty("sourceSpec", out var ss) ? ss.GetString() : null
                });
            }
        }

        // Parse models
        if (doc.RootElement.TryGetProperty("models", out var modelsElement))
        {
            foreach (var model in modelsElement.EnumerateArray())
            {
                data.Models.Add(new ModelEntry
                {
                    Name = model.TryGetProperty("name", out var n) ? n.GetString() : null,
                    Kind = model.TryGetProperty("kind", out var k) ? k.GetString() : null,
                    PropertyCount = model.TryGetProperty("propertyCount", out var pc) ? pc.GetInt32() : 0,
                    MatchType = model.TryGetProperty("matchType", out var mt) ? mt.GetString() : null,
                    Program = model.TryGetProperty("program", out var p) ? p.GetString() : null,
                    SchemaSource = model.TryGetProperty("schemaSource", out var ss) ? ss.GetString() : null
                });
            }
        }

        // Compute per-program stats
        var programIds = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10" };
        foreach (var pid in programIds)
        {
            var ops = data.Operations.Where(o => o.Program == pid).ToList();
            var models = data.Models.Where(m => m.Program == pid).ToList();
            data.ProgramStats[pid] = new ProgramStats
            {
                MatchedOperations = ops.Count(o => o.MatchStatus == "Matched"),
                UnmatchedOperations = ops.Count(o => o.MatchStatus != "Matched"),
                MatchedModels = models.Count(m => m.MatchType == "Matched"),
                UnmatchedModels = models.Count(m => m.MatchType != "Matched")
            };
        }

        _cachedData = data;
        return data;
    }
}
