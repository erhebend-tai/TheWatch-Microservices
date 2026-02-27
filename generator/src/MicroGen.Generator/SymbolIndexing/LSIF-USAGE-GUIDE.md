# Symbol Indexing (LSIF) Usage Guide

The MicroGen Symbol Indexing system uses .NET Reflection to extract semantic information from compiled assemblies and generate [Language Server Index Format (LSIF)](https://microsoft.github.io/language-server-protocol/specifications/lsif/0.4.0/specification/) documents. These documents enable IDE navigation features like Go to Definition, Find References, and Type Hierarchy visualization.

## Overview

The symbol indexing system consists of four main components:

1. **SymbolIndexGenerator** — Walks a compiled assembly using Reflection to extract all types, members, and relationships
2. **SymbolIndexQueryService** — Provides query operations on an indexed LSIF document (find, search, navigate)
3. **LsifDocumentWriter** — Serializes LSIF data to JSON and exports markdown documentation
4. **SymbolIndexingService** — Orchestrates the full workflow (generate, write, validate, cache)

## Basic Usage

### 1. Generate Index from Assembly

```csharp
using MicroGen.Generator.SymbolIndexing;
using Microsoft.Extensions.Logging;

// Create logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<SymbolIndexingService>();

// Create service
var indexingService = new SymbolIndexingService(logger);

// Index an assembly
var result = await indexingService.IndexAssemblyAsync(
    assemblyPath: "bin/Release/MyService.Api.dll",
    outputDirectory: ".lsif",
    options: new IndexingOptions
    {
        IncludeInternals = true,
        IncludePrivate = false,
        ExportMarkdown = true
    }
);

if (result.Success)
{
    Console.WriteLine($"Indexed {result.SymbolCount} symbols");
    Console.WriteLine($"LSIF written to: {result.LsifPath}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### 2. Query Symbol Information

```csharp
// Load the LSIF document
var json = File.ReadAllText(".lsif/MyService.Api.lsif.json");
var document = JsonSerializer.Deserialize<LsifDocument>(json);

// Create query service
var queryService = new SymbolIndexQueryService(logger);
queryService.LoadIndex(document);

// Find a specific type
var symbol = queryService.FindSymbol("MyService.Api.Controllers.OrderController");

// Get all members of a type
var members = queryService.GetTypeMembers("MyService.Api.Controllers.OrderController", 
    includeInherited: true);

// Find all references
var references = queryService.FindReferences("MyService.Domain.Order");

// Get type hierarchy
var hierarchy = queryService.GetTypeHierarchy("MyService.Domain.Order");

// Search with criteria
var results = queryService.Search(new SymbolSearchCriteria
{
    NamePattern = "Controller",
    Kind = "class",
    FilterAccessLevel = "public"
});

// Get hover information
var hover = queryService.GetHoverInfo("MyService.Api.Controllers.OrderController.GetOrderById");
Console.WriteLine(hover.Content);
```

### 3. Generate Reports

```csharp
// Generate a symbol report
var report = indexingService.GenerateSymbolReport(".lsif/MyService.Api.lsif.json");

Console.WriteLine("Symbol Counts by Kind:");
foreach (var (kind, count) in report.CountByKind)
{
    Console.WriteLine($"  {kind}: {count}");
}

Console.WriteLine("\nPublic API Surface:");
foreach (var symbol in report.PublicSymbols.Take(10))
{
    Console.WriteLine($"  {symbol.FullyQualifiedName}");
}

Console.WriteLine("\nLargest Types:");
foreach (var (symbol, memberCount) in report.LargeTypes.Take(5))
{
    Console.WriteLine($"  {symbol.Name}: {memberCount} members");
}
```

## Integration with MicroGen

The generator automatically invokes symbol indexing after building assemblies if enabled:

```csharp
// In your GeneratorConfig
var config = new GeneratorConfig
{
    OutputDirectory = "./services",
    Features = new FeatureConfig
    {
        SymbolIndexing = true  // Enable LSIF generation
    }
};

// Pass indexing service to SolutionGenerator
var indexingService = new SymbolIndexingService(logger);
var solutionGenerator = new SolutionGenerator(logger, config, indexingService);

// Generate as usual - symbol indexes are created automatically
var result = await solutionGenerator.GenerateServiceAsync(service);
```

The LSIF files are written to `.lsif/` subdirectory within each service:

```
MyService/
├── bin/
├── .lsif/
│   ├── MyService.Api.lsif.json      # Full LSIF document
│   ├── MyService.Api.symbols.md     # Markdown documentation
│   ├── MyService.Domain.lsif.json
│   └── MyService.Domain.symbols.md
└── src/
```

## LSIF Document Structure

An LSIF document is a JSON file containing:

```json
{
  "metadata": {
    "projectRoot": "/path/to/project",
    "toolInfo": {
      "name": "MicroGen.SymbolIndexer",
      "version": "1.0.0"
    },
    "generatedAt": "2024-01-15T10:30:00Z"
  },
  "symbols": [
    {
      "id": "element/1",
      "name": "OrderController",
      "fullyQualifiedName": "MyService.Api.Controllers.OrderController",
      "kind": "class",
      "accessLevel": "public",
      "isStatic": false,
      "isAbstract": false,
      "isVirtual": false,
      "isOverride": false,
      "signature": "class OrderController : ControllerBase",
      "baseTypes": ["ControllerBase"],
      "implementedInterfaces": [],
      "genericConstraints": []
    }
    // ... more symbols
  ],
  "references": {
    "element/1": [
      { "targetId": "element/42", "kind": "reference" }
    ]
  }
}
```

## Symbol Kinds

| Kind | Example |
|------|---------|
| `class` | `public class OrderService` |
| `interface` | `public interface IOrderService` |
| `struct` | `public struct OrderId` |
| `enum` | `public enum OrderStatus` |
| `method` | `public async Task<Order> GetOrderAsync(int id)` |
| `property` | `public string Name { get; set; }` |
| `field` | `private readonly ILogger _logger;` |
| `event` | `public event EventHandler Changed;` |

## Access Levels

- `public` — Accessible from anywhere
- `protected` — Accessible from derived types
- `internal` — Accessible within the same assembly
- `private` — Accessible only within the same type

## Symbol Modifiers

Symbols can have these boolean modifiers:

- `isStatic` — Static members
- `isAbstract` — Abstract types and methods
- `isVirtual` — Virtual methods (can be overridden)
- `isOverride` — Members overriding a base implementation

## Advanced: Batch Indexing

Index all assemblies in a directory:

```csharp
var aggregated = await indexingService.IndexDirectoryAsync(
    directoryPath: "bin/Release",
    outputDirectory: ".lsif",
    options: new IndexingOptions { ExportMarkdown = true }
);

Console.WriteLine($"Indexed {aggregated.Results.Count} assemblies");
Console.WriteLine($"Total symbols: {aggregated.TotalSymbols}");
Console.WriteLine($"Time: {aggregated.TotalDuration.TotalSeconds:F2}s");

// Check results
foreach (var (assemblyPath, result) in aggregated.Results)
{
    if (result.Success)
    {
        Console.WriteLine($"✓ {Path.GetFileName(assemblyPath)}: {result.SymbolCount} symbols");
    }
    else
    {
        Console.WriteLine($"✗ {Path.GetFileName(assemblyPath)}: {result.Error}");
    }
}
```

## Caching

The system caches generated indexes by assembly file timestamp. If you regenerate assemblies without changing them, the cache is reused:

```csharp
// First call — generates index
var result1 = await indexingService.IndexAssemblyAsync(dllPath, ".lsif");
// result1.FromCache == false, took 500ms

// Second call with same assembly — uses cache
var result2 = await indexingService.IndexAssemblyAsync(dllPath, ".lsif");
// result2.FromCache == true, took 5ms

// Clear cache when needed
var cache = new LsifDocumentCache(logger);
cache.Clear();
```

## Markdown Export Example

When `ExportMarkdown` is enabled, a human-readable `.symbols.md` file is created:

```markdown
# Symbol Index

Generated: 2024-01-15T10:30:00Z
Tool: MicroGen.SymbolIndexer v1.0.0

## Summary
- Total Symbols: 156

## CLASS (45)

### MyService.Api.Controllers.OrderController
Controller for order management operations

- Access: `public`
- Extends: ControllerBase
- Signature: `class OrderController : ControllerBase`

### MyService.Domain.Order
- Access: `public`
- Signature: `class Order : Entity<OrderId>`
- Extends: Entity<OrderId>

...
```

## API Query Examples

### Find Type Hierarchies

```csharp
// Get the inheritance tree
var hierarchy = queryService.GetTypeHierarchy("MyService.Domain.OrderAggregate");

Console.WriteLine($"Type: {hierarchy.Type.Name}");
Console.WriteLine("Extends:");
hierarchy.BaseTypes.ForEach(t => Console.WriteLine($"  - {t.Name}"));
Console.WriteLine("Implemented by:");
hierarchy.ImplementingTypes.ForEach(t => Console.WriteLine($"  - {t.Name}"));
```

### Search for Symbols

```csharp
// Find all public methods containing "Order"
var results = queryService.Search(new SymbolSearchCriteria
{
    NamePattern = "Order",
    Kind = "method",
    FilterAccessLevel = "public"
});

foreach (var symbol in results)
{
    Console.WriteLine($"{symbol.Kind} {symbol.FullyQualifiedName}");
}
```

### Cross-Reference Analysis

```csharp
// Find what uses a specific type
var usages = queryService.FindReferences("MyService.Domain.Order");

foreach (var usage in usages)
{
    Console.WriteLine($"Used in: {usage.FullyQualifiedName}");
}
```

## Performance Considerations

- **First Index**: ~500ms for medium assemblies (100-200 symbols)
- **Cached Lookup**: ~5ms
- **Query Operations**: <10ms for most queries (find, search)
- **Hierarchy Traversal**: ~20ms for deep hierarchies

## Troubleshooting

### "Index not loaded" Exception

Ensure you call `LoadIndex()` before querying:

```csharp
queryService.LoadIndex(document);  // Required first
var symbol = queryService.FindSymbol("MyType");
```

### Missing Internal/Private Symbols

Check `IndexingOptions`:

```csharp
var options = new IndexingOptions
{
    IncludeInternals = true,   // Include internal types
    IncludePrivate = true      // Include private members
};
```

### Assembly Not Found

Ensure the assembly path is correct and the assembly has been compiled:

```csharp
if (!File.Exists(assemblyPath))
    throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
```

## Integration with IDE

The LSIF documents enable these IDE features:

- **Go to Definition**: Jump to symbol declaration
- **Find References**: Locate all usages
- **Type Hierarchy**: Visualize inheritance trees
- **Hover Information**: Show type and member details
- **Code Completion**: IntelliSense suggestions
- **Rename Refactoring**: Safe symbol renaming

See [LSP Specification](https://microsoft.github.io/language-server-protocol/) for details on implementing these features.
