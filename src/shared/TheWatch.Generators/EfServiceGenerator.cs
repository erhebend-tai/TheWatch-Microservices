using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TheWatch.Generators;

/// <summary>
/// Roslyn incremental generator that emits:
///   1. Typed query extensions per entity  (GetByForeignKeyAsync, FilterByEnum, PagedListAsync)
///   2. Auto-migration middleware           (UseWatchMigrations)
///   3. Seed data extension point           (IWatchDataSeeder)
/// Shares the same entity-detection heuristic as DbContextGenerator (Guid Id).
/// </summary>
[Generator]
public class EfServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var projectInfo = context.AnalyzerConfigOptionsProvider
            .Select((opts, _) =>
            {
                opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                opts.GlobalOptions.TryGetValue("build_property.UseMaui", out var useMaui);
                return new ProjectMeta(ns ?? "TheWatch", useMaui ?? "");
            });

        var entities = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds &&
                    cds.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                    !cds.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    !cds.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: static (ctx, ct) => ExtractEntityInfo(ctx, ct))
            .Where(static e => e != null)
            .Select(static (e, _) => e!.Value)
            .Collect();

        var combined = projectInfo.Combine(entities);
        context.RegisterSourceOutput(combined, static (spc, data) =>
        {
            var (meta, entityList) = data;
            Generate(spc, meta, entityList);
        });
    }

    private static EntityInfo? ExtractEntityInfo(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var filePath = ctx.Node.SyntaxTree.FilePath ?? "";
        if (filePath.Replace('/', '\\').Contains("\\obj\\"))
            return null;

        var cds = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(cds, ct) is not INamedTypeSymbol symbol)
            return null;

        var ns = symbol.ContainingNamespace.ToDisplayString();
        if (ns.EndsWith(".Models"))
            return null;

        var allProps = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                        !p.IsStatic && p.GetMethod != null && p.SetMethod != null)
            .ToList();

        var hasGuidId = allProps.Any(p => p.Name == "Id" && IsGuidType(p.Type));
        if (!hasGuidId) return null;

        var propInfos = allProps
            .Select(p => new PropData(p.Name, GetDisplayTypeName(p.Type), ClassifyProp(p.Type)))
            .ToArray();

        return new EntityInfo(symbol.Name, ns, propInfos);
    }

    private static bool IsGuidType(ITypeSymbol type)
        => type.Name == "Guid" && type.ContainingNamespace is { Name: "System" };

    private static string GetDisplayTypeName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    private static PropKind ClassifyProp(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nts && nts.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return ClassifyProp(nts.TypeArguments[0]);

        if (type.TypeKind == TypeKind.Enum) return PropKind.Enum;

        if (type.Name is "Guid" && type.ContainingNamespace is { Name: "System" })
            return PropKind.ForeignKey;

        if (type.SpecialType == SpecialType.System_Boolean)
            return PropKind.Boolean;

        if (type.SpecialType == SpecialType.System_DateTime ||
            type.Name is "DateTimeOffset" or "DateTime")
            return PropKind.DateTime;

        return PropKind.Other;
    }

    private static void Generate(SourceProductionContext spc, ProjectMeta meta,
        ImmutableArray<EntityInfo> entities)
    {
        var rootNs = meta.Namespace;
        if (rootNs.Contains(".Tests") || rootNs.Contains(".Generators") ||
            rootNs.Contains(".Shared") || rootNs.Contains(".AppHost") ||
            rootNs.Contains(".ServiceDefaults") || rootNs.Contains(".Dashboard"))
            return;
        if (meta.UseMaui.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            rootNs.Contains(".Mobile"))
            return;

        var program = GeneratorHelpers.DetectProgram(rootNs);
        if (program == null) return;
        if (entities.Length == 0) return;

        var seen = new HashSet<string>();
        var unique = new List<EntityInfo>();
        foreach (var e in entities)
            if (seen.Add(e.Name)) unique.Add(e);
        if (unique.Count == 0) return;

        var parts = rootNs.Split('.');
        var shortName = parts.Length >= 3 ? parts[parts.Length - 1] : program;
        var contextName = shortName + "DbContext";
        var entityNamespaces = unique.Select(e => e.Namespace).Distinct().ToList();

        // Emit query extensions
        var sb = new StringBuilder(8192);
        WriteQueryExtensions(sb, rootNs, contextName, unique, entityNamespaces);
        spc.AddSource("RepositoryExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

        // Emit migration + seeder setup
        var sb2 = new StringBuilder(4096);
        WriteMigrationSetup(sb2, rootNs, contextName);
        spc.AddSource("MigrationSetup.g.cs", SourceText.From(sb2.ToString(), Encoding.UTF8));
    }

    // ─── Query Extensions ───

    private static void WriteQueryExtensions(StringBuilder sb, string rootNs, string ctxName,
        List<EntityInfo> entities, List<string> entityNs)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Repository query extensions generated by TheWatch.Generators.EfServiceGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        foreach (var ns in entityNs.Where(n => n != rootNs).OrderBy(n => n))
            sb.AppendLine($"using {ns};");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNs};");
        sb.AppendLine();

        // PagedResult<T> DTO
        sb.AppendLine("/// <summary>Generic paged result for list queries.</summary>");
        sb.AppendLine("public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);");
        sb.AppendLine();

        // IWatchDataSeeder interface
        sb.AppendLine("/// <summary>Implement this to seed development data on startup.</summary>");
        sb.AppendLine("public interface IWatchDataSeeder");
        sb.AppendLine("{");
        sb.AppendLine($"    Task SeedAsync({ctxName} context, CancellationToken ct = default);");
        sb.AppendLine("}");
        sb.AppendLine();

        // Per-entity extension class
        foreach (var entity in entities)
        {
            sb.AppendLine($"/// <summary>Typed query extensions for {entity.Name}.</summary>");
            sb.AppendLine($"public static class {entity.Name}RepositoryExtensions");
            sb.AppendLine("{");

            // FK lookups: properties ending in "Id" that are Guid type (excluding "Id" itself)
            var fkProps = entity.Properties
                .Where(p => p.Kind == PropKind.ForeignKey && p.Name != "Id" && p.Name.EndsWith("Id"))
                .ToList();

            foreach (var fk in fkProps)
            {
                var baseName = fk.Name.Substring(0, fk.Name.Length - 2); // Remove "Id"
                if (string.IsNullOrEmpty(baseName)) baseName = fk.Name;
                var nullable = fk.TypeName.EndsWith("?");

                sb.AppendLine($"    /// <summary>Find all {entity.Name} by {fk.Name}.</summary>");
                sb.AppendLine($"    public static async Task<List<{entity.Name}>> GetBy{baseName}Async(");
                sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, Guid {GeneratorHelpers.CamelCase(fk.Name)}, CancellationToken ct = default)");
                sb.AppendLine($"        => await repo.Query().Where(e => e.{fk.Name} == {GeneratorHelpers.CamelCase(fk.Name)}).ToListAsync(ct);");
                sb.AppendLine();
            }

            // Enum filters
            var enumProps = entity.Properties
                .Where(p => p.Kind == PropKind.Enum)
                .ToList();

            foreach (var ep in enumProps)
            {
                sb.AppendLine($"    /// <summary>Filter {entity.Name} by {ep.Name}.</summary>");
                sb.AppendLine($"    public static async Task<List<{entity.Name}>> GetBy{ep.Name}Async(");
                sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, {ep.TypeName.TrimEnd('?')} {GeneratorHelpers.CamelCase(ep.Name)}, CancellationToken ct = default)");
                sb.AppendLine($"        => await repo.Query().Where(e => e.{ep.Name} == {GeneratorHelpers.CamelCase(ep.Name)}).ToListAsync(ct);");
                sb.AppendLine();
            }

            // Boolean filters
            var boolProps = entity.Properties
                .Where(p => p.Kind == PropKind.Boolean && p.Name != "Id")
                .ToList();

            foreach (var bp in boolProps)
            {
                sb.AppendLine($"    /// <summary>Filter {entity.Name} where {bp.Name} is true.</summary>");
                sb.AppendLine($"    public static async Task<List<{entity.Name}>> GetWhere{bp.Name}Async(");
                sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, CancellationToken ct = default)");
                sb.AppendLine($"        => await repo.Query().Where(e => e.{bp.Name}).ToListAsync(ct);");
                sb.AppendLine();
            }

            // Paged list with optional ordering by CreatedAt (if present)
            var hasCreatedAt = entity.Properties.Any(p => p.Name == "CreatedAt");
            var orderClause = hasCreatedAt
                ? ".OrderByDescending(e => e.CreatedAt)"
                : "";

            sb.AppendLine($"    /// <summary>Paged list of {entity.Name}.</summary>");
            sb.AppendLine($"    public static async Task<PagedResult<{entity.Name}>> PagedListAsync(");
            sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, int page = 1, int pageSize = 20, CancellationToken ct = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var query = repo.Query(){orderClause};");
            sb.AppendLine("        var total = await query.CountAsync(ct);");
            sb.AppendLine("        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);");
            sb.AppendLine($"        return new PagedResult<{entity.Name}>(items, total, page, pageSize);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // DateTime range query for CreatedAt
            if (hasCreatedAt)
            {
                sb.AppendLine($"    /// <summary>Query {entity.Name} created within a time range.</summary>");
                sb.AppendLine($"    public static async Task<List<{entity.Name}>> GetCreatedBetweenAsync(");
                sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, DateTime from, DateTime to, CancellationToken ct = default)");
                sb.AppendLine($"        => await repo.Query().Where(e => e.CreatedAt >= from && e.CreatedAt <= to).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);");
                sb.AppendLine();
            }

            // Count
            sb.AppendLine($"    /// <summary>Count all {entity.Name} entities.</summary>");
            sb.AppendLine($"    public static async Task<int> CountAsync(");
            sb.AppendLine($"        this IWatchRepository<{entity.Name}> repo, CancellationToken ct = default)");
            sb.AppendLine($"        => await repo.Query().CountAsync(ct);");

            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    // ─── Migration Setup ───

    private static void WriteMigrationSetup(StringBuilder sb, string rootNs, string ctxName)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Migration + seeding setup generated by TheWatch.Generators.EfServiceGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNs};");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Auto-migration and seeding middleware for {ctxName}.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class MigrationSetup");
        sb.AppendLine("{");

        // UseWatchMigrations
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Applies pending EF Core migrations on startup.");
        sb.AppendLine("    /// In development, also runs IWatchDataSeeder if registered.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static async Task<WebApplication> UseWatchMigrations(this WebApplication app)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var scope = app.Services.CreateScope();");
        sb.AppendLine($"        var db = scope.ServiceProvider.GetRequiredService<{ctxName}>();");
        sb.AppendLine("        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(\"MigrationSetup\");");
        sb.AppendLine();
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            var pending = await db.Database.GetPendingMigrationsAsync();");
        sb.AppendLine("            var pendingList = pending as string[] ?? System.Linq.Enumerable.ToArray(pending);");
        sb.AppendLine("            if (pendingList.Length > 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                logger.LogInformation(\"Applying {Count} pending migrations...\", pendingList.Length);");
        sb.AppendLine("                await db.Database.MigrateAsync();");
        sb.AppendLine("                logger.LogInformation(\"Migrations applied successfully.\");");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                logger.LogInformation(\"Database is up to date — no pending migrations.\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            logger.LogWarning(ex, \"Migration failed — database may not be available yet. Ensure EnsureCreated for dev.\");");
        sb.AppendLine("            try { await db.Database.EnsureCreatedAsync(); }");
        sb.AppendLine("            catch { /* Swallow if DB truly unavailable */ }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Run seeder if registered");
        sb.AppendLine("        var seeder = scope.ServiceProvider.GetService<IWatchDataSeeder>();");
        sb.AppendLine("        if (seeder is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            logger.LogInformation(\"Running data seeder...\");");
        sb.AppendLine("            await seeder.SeedAsync(db);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return app;");
        sb.AppendLine("    }");

        sb.AppendLine("}");
    }

    // ─── Pipeline Data Types ───

    private struct ProjectMeta : IEquatable<ProjectMeta>
    {
        public string Namespace;
        public string UseMaui;
        public ProjectMeta(string ns, string useMaui) { Namespace = ns; UseMaui = useMaui; }
        public bool Equals(ProjectMeta other) => Namespace == other.Namespace && UseMaui == other.UseMaui;
        public override bool Equals(object? obj) => obj is ProjectMeta o && Equals(o);
        public override int GetHashCode() => (Namespace?.GetHashCode() ?? 0) ^ (UseMaui?.GetHashCode() ?? 0);
    }

    private struct EntityInfo : IEquatable<EntityInfo>
    {
        public string Name;
        public string Namespace;
        public PropData[] Properties;
        public EntityInfo(string name, string ns, PropData[] props) { Name = name; Namespace = ns; Properties = props; }
        public bool Equals(EntityInfo other)
        {
            if (Name != other.Name || Namespace != other.Namespace) return false;
            if (Properties == null && other.Properties == null) return true;
            if (Properties == null || other.Properties == null) return false;
            if (Properties.Length != other.Properties.Length) return false;
            for (int i = 0; i < Properties.Length; i++)
                if (!Properties[i].Equals(other.Properties[i])) return false;
            return true;
        }
        public override bool Equals(object? obj) => obj is EntityInfo o && Equals(o);
        public override int GetHashCode()
        {
            int h = (Name?.GetHashCode() ?? 0) ^ (Namespace?.GetHashCode() ?? 0);
            if (Properties != null) foreach (var p in Properties) h = h * 31 + p.GetHashCode();
            return h;
        }
    }

    private struct PropData : IEquatable<PropData>
    {
        public string Name;
        public string TypeName;
        public PropKind Kind;
        public PropData(string name, string typeName, PropKind kind) { Name = name; TypeName = typeName; Kind = kind; }
        public bool Equals(PropData other) => Name == other.Name && TypeName == other.TypeName && Kind == other.Kind;
        public override bool Equals(object? obj) => obj is PropData o && Equals(o);
        public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ (TypeName?.GetHashCode() ?? 0) ^ (int)Kind;
    }

    private enum PropKind { Other, Enum, ForeignKey, Boolean, DateTime }
}
