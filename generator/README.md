# MicroGen vs TheWatch.Generators — Analysis

## Purpose

This document analyzes the **MicroGen** code generator (in `generator/`) — a recycled earlier-iteration tool — and compares it with the current **TheWatch.Generators** Roslyn source generator to identify features worth adopting.

---

## Architecture Comparison

| Aspect | TheWatch.Generators | MicroGen |
|--------|---------------------|----------|
| **Type** | Roslyn incremental source generator (`IIncrementalGenerator`) | Standalone CLI application |
| **Runtime** | Runs at compile time inside the C# compiler | Runs on demand via `dotnet run` |
| **Target** | netstandard2.0 | .NET 10.0 |
| **Template engine** | Inline `StringBuilder` / string interpolation | Scriban templates |
| **Input** | JSON specs + live Roslyn syntax trees | OpenAPI, SQL, CSV, GraphQL, HTML files |
| **Output** | `*.g.cs` source files injected into compilation | Full project trees on disk (`.csproj`, `.cs`, `.razor`, YAML, Dockerfile) |
| **Scope** | Generates code _within_ existing projects | Generates _entire_ projects and solutions |

### Key Takeaway

TheWatch.Generators operates at **compile time** and augments existing projects with generated code. MicroGen operates at **design time** and scaffolds entire project trees from spec files. They are complementary, not competing.

---

## What MicroGen Has That TheWatch.Generators Does Not

### 1. Multi-Format Parsing

MicroGen supports five input formats through a polymorphic `ISourceParser` interface:

| Parser | Extensions | What It Extracts |
|--------|------------|------------------|
| **OpenAPI (SpecParser)** | `.yaml`, `.yml`, `.json` | Operations, schemas, tags → `ServiceDescriptor` |
| **SQL (SqlParser)** | `.sql` | Tables → CRUD operations via TSql170 AST (SQL Server 2022 TransactSql.ScriptDom) |
| **CSV (CsvParser)** | `.csv` | Function catalogs or data tables → operations |
| **GraphQL (GraphQlParser)** | `.gql`, `.graphql` | Queries/mutations → operations |
| **HTML (WebsiteParser)** | `.html`, `.htm` | Forms and data → operations via AngleSharp |

**Relevance:** TheWatch.Generators reads only from pre-built JSON specs (`_mapping.json`, `Controllers.json`, `Models.json`, `Interfaces.json`) and Roslyn syntax trees. Adding direct OpenAPI or SQL parsing would let us generate code without the `scripts/map_specs_to_code.py` intermediary step. However, since the Roslyn generator runs at compile time, heavy parsing libraries may not be appropriate there — this capability is better suited to a pre-build CLI step.

**Recommendation:** ⚠️ **Consider but don't adopt directly.** The JSON-spec approach in TheWatch.Generators is already effective. Adding parsing libraries (e.g., `Microsoft.OpenApi`, `YamlDotNet`, `AngleSharp`) to a Roslyn source generator would increase compile-time overhead and introduce large dependency trees into the compiler pipeline. If direct OpenAPI parsing is desired, it should be a separate CLI tool (like MicroGen itself) rather than a compile-time generator.

---

### 2. Scriban Template Engine

MicroGen uses [Scriban](https://github.com/scriban/scriban) for all code generation, with helper functions (`PascalCase`, `CamelCase`, `KebabCase`, `Indent`) and model injection.

**Relevance:** TheWatch.Generators uses inline `StringBuilder` and string interpolation (e.g., `sb.AppendLine($"public class {name}")`), which works but becomes harder to maintain as templates grow complex.

**Recommendation:** ✅ **Adopt for complex generators.** Generators like `RestApiControllerGenerator`, `DbContextGenerator`, and `TestGenerator` produce large blocks of code that would benefit from external templates. Simpler generators (e.g., `OpenApiGenerator`, `SerilogGenerator`) are fine with string interpolation.

---

### 3. Blazor Dashboard Scaffolding with Radzen

MicroGen generates complete Blazor dashboards per service with Radzen components:

- **CRUD pages:** `{Entity}List.razor`, `{Entity}Form.razor`, `{Entity}Detail.razor`
- **Metrics dashboard:** Line/Area/Donut charts, KPI cards, real-time refresh
- **Advanced filter dialog:** Full-text search, date ranges, status filters
- **Export service:** CSV/JSON/Excel via JS interop
- **Enhanced validation:** `RadzenRequiredValidator`, `RadzenEmailValidator`, `RadzenLengthValidator`

**Relevance:** TheWatch.Generators has no Blazor page generation. The `generated-blazor-pages/` directory was produced by a separate Python script (`scripts/generate-blazor-pages.py`), not by either generator. MicroGen's approach is more integrated and produces richer output.

**Recommendation:** ✅ **Adopt the Radzen page generation patterns.** Add a Blazor page generator to TheWatch.Generators that emits `.razor` and `.razor.cs` files using the entity detection already in place (`DbContextGenerator` already finds entities with `Guid Id`). Prioritize list and form pages since they cover the most common use cases.

---

### 4. Kubernetes / Helm / Docker Scaffolding

MicroGen generates deployment infrastructure:

- `Dockerfile` and `.dockerignore` per service
- Kubernetes manifests: `Namespace`, `Deployment`, `Service`, `ConfigMap`, `HPA`
- Helm charts with `Chart.yaml` and `values.yaml`
- `docker-compose.yml` for local development
- GitHub Actions CI/CD workflows

**Relevance:** TheWatch already has `docker/`, `helm/`, `terraform/`, and `docker-compose.yml` files, but they were hand-written. Auto-generating these from service metadata would reduce drift between services and infrastructure.

**Recommendation:** ⚠️ **Consider selectively.** Generating Dockerfiles and Helm values from project metadata is valuable, but this is better as a standalone CLI tool than a Roslyn source generator (it produces non-C# files). MicroGen's templates could be reused for this purpose.

---

### 5. YARP API Gateway Generation

MicroGen generates a .NET YARP (Yet Another Reverse Proxy) API gateway configuration that routes to all discovered services.

**Relevance:** TheWatch.P1.CoreGateway could benefit from auto-generated route configuration based on discovered services and their OpenAPI specs.

**Recommendation:** ⚠️ **Consider.** Useful but a niche feature. Could be added to the existing `MicroserviceEndpointGenerator` or as a new generator that emits YARP route configuration.

---

### 6. Apache Stack Integration (Kafka, Dubbo, Superset, ECharts)

MicroGen generates configuration for:

- **Kafka** via Strimzi/Bitnami (topic definitions, consumer groups)
- **Apache Dubbo** service registry (NACOS)
- **Apache Superset** analytics dashboards
- **Apache ECharts** chart integration

**Relevance:** TheWatch.Generators already has a `KafkaEventBusGenerator` that produces producer/consumer code. MicroGen adds infrastructure-level Kafka deployment manifests (Strimzi CRDs) and additional Apache ecosystem integrations.

**Recommendation:** ⚠️ **Low priority.** The existing Kafka generator covers the application-level needs. Infrastructure manifests belong in the `helm/` or `terraform/` directories.

---

### 7. Dapr Integration

MicroGen generates Dapr sidecar configuration:

- State store components
- Pub/sub components
- Secret store components

**Relevance:** Dapr could complement the existing Kafka and SignalR patterns for service-to-service communication.

**Recommendation:** ⚠️ **Consider for future.** Dapr adoption is a broader architectural decision, not just a generator feature.

---

### 8. LSIF Symbol Indexing

MicroGen includes a `SymbolIndexingService` that generates Language Server Index Format (LSIF) data for:

- Cross-reference navigation
- Hover documentation
- Go-to-definition support

**Relevance:** This would improve the developer experience when navigating generated code in IDEs and GitHub Code Search.

**Recommendation:** ⚠️ **Interesting but low priority.** Generated `.g.cs` files from Roslyn are already navigable in IDEs. LSIF is more useful for the MicroGen-style disk-output generators.

---

### 9. Dry-Run / Scan Mode

MicroGen provides `scan` and `--dry-run` commands that preview what would be generated without writing files, displaying results in a Spectre.Console tree/table.

**Relevance:** TheWatch.Generators runs at compile time so there's no standalone "preview" concept, but a diagnostic analyzer that reports what will be generated could serve a similar purpose.

**Recommendation:** ⚠️ **Nice to have.** Could be implemented as a Roslyn diagnostic analyzer that lists generated files, but the effort may not justify the benefit.

---

### 10. OpenAPI Spec Validation

MicroGen includes a `validate` command that checks OpenAPI specs for correctness, with a `--strict` mode that treats warnings as errors.

**Relevance:** This would be useful in CI pipelines to catch spec errors before they reach the generator.

**Recommendation:** ✅ **Adopt as a CI step.** This doesn't need to be in TheWatch.Generators itself — it should be a CI workflow step using MicroGen's validate command or a standalone tool.

---

### 11. Aspire AppHost Orchestration

MicroGen generates a .NET Aspire AppHost that wires up all discovered services with:

- Service discovery
- Health checks
- Dashboard configuration

**Relevance:** TheWatch already has `TheWatch.Aspire.AppHost/Program.cs`, but it is manually maintained. Auto-generating the service registrations from discovered projects would keep it in sync.

**Recommendation:** ✅ **Adopt.** Add a generator that emits Aspire AppHost registration code based on detected services. This fits naturally as a Roslyn source generator.

---

## What TheWatch.Generators Has That MicroGen Does Not

These are strengths of the existing generator that should be preserved:

| Feature | Details |
|---------|---------|
| **Incremental compilation** | Roslyn caching via `IEquatable<T>` — only regenerates when inputs change |
| **Entity auto-detection** | Finds entities by scanning for `public Guid Id` properties in source code |
| **Property classification** | Categorizes properties (FK, Enum, Boolean, DateTime, Geometry, etc.) for targeted code |
| **EF Core repository extensions** | Generates type-aware query methods (FK lookups, enum filters, paged queries, time-range queries) |
| **SignalR hub generation** | Creates typed hubs with group management and CRUD broadcasters per entity |
| **Hangfire job classification** | Auto-classifies operations by keyword (upload → fire-and-forget, health → recurring) |
| **MAUI-aware generation** | Detects MAUI projects and generates mobile-specific clients and routes |
| **FCM notification channels** | Maps programs to notification channels (P2→Emergency, P6→Dispatch) |
| **Zero external dependencies** | Relies only on `Microsoft.CodeAnalysis` — no extra NuGet packages needed |

---

## Recommended Adoption Roadmap

### Phase 1 — Quick Wins (Low effort, high value)

| # | Feature | Source | Target | Effort |
|---|---------|--------|--------|--------|
| 1 | **OpenAPI validation in CI** | MicroGen `validate` command | `.github/workflows/ci.yml` | Small |
| 2 | **Aspire AppHost auto-registration** | MicroGen Aspire generator | New generator in TheWatch.Generators | Medium |
| 3 | **Radzen Blazor list/form pages** | MicroGen `RadzenPageGenerator` | New generator in TheWatch.Generators | Medium |

### Phase 2 — Medium-Term Improvements

| # | Feature | Source | Target | Effort |
|---|---------|--------|--------|--------|
| 4 | **Scriban templates** for complex generators | MicroGen template engine | Refactor `RestApiControllerGenerator`, `DbContextGenerator` | Medium |
| 5 | **Export service generation** (CSV/JSON/Excel) | MicroGen `ExportService` template | New generator in TheWatch.Generators | Small |
| 6 | **Metrics dashboard scaffolding** | MicroGen metrics template | Extend Blazor page generator | Small |

### Phase 3 — Strategic Additions

| # | Feature | Source | Target | Effort |
|---|---------|--------|--------|--------|
| 7 | **Dockerfile generation** | MicroGen Docker templates | Standalone CLI or build target | Medium |
| 8 | **Helm chart generation** | MicroGen Helm templates | Standalone CLI or build target | Medium |
| 9 | **YARP gateway routes** | MicroGen gateway generator | New generator in TheWatch.Generators | Medium |
| 10 | **Dapr component scaffolding** | MicroGen Dapr config | Future architectural decision | Large |

---

## Features Not Recommended for Adoption

| Feature | Reason |
|---------|--------|
| **Multi-format parsing (SQL, CSV, GraphQL, HTML)** | Adds heavy dependencies to compile-time generator; the existing JSON-spec pipeline is sufficient |
| **LSIF symbol indexing** | Generated Roslyn source is already IDE-navigable |
| **Full solution scaffolding** | TheWatch.Generators augments existing projects; solution-level scaffolding is a one-time setup task already handled by `scripts/generate_projects.py` |
| **Dry-run / scan mode** | Roslyn generators don't have a standalone execution model; a diagnostic analyzer is possible but low value |

---

## Conclusion

MicroGen is a comprehensive **design-time scaffolding tool** while TheWatch.Generators is a **compile-time code augmentation engine**. The most valuable features to bring over are:

1. **Radzen Blazor page generation** — fills the biggest gap in TheWatch.Generators
2. **Aspire AppHost auto-registration** — eliminates manual service wiring
3. **OpenAPI validation** — catches spec errors early in CI
4. **Scriban templates** — improves maintainability for complex generators
5. **Export and metrics scaffolding** — adds immediate end-user value to generated dashboards

These five additions would bring the most impactful MicroGen capabilities into the existing generator without disrupting its compile-time architecture or adding unnecessary complexity.
