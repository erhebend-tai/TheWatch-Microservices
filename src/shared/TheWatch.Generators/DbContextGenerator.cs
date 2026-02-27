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

[Generator]
public class DbContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var projectInfo = context.AnalyzerConfigOptionsProvider
            .Select((opts, _) =>
            {
                opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                opts.GlobalOptions.TryGetValue("build_property.UseMaui", out var useMaui);
                opts.GlobalOptions.TryGetValue("build_property.WatchDbProvider", out var dbProvider);
                return new ProjectMeta(ns ?? "TheWatch", useMaui ?? "", dbProvider ?? "SqlServer");
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
        context.RegisterSourceOutput(combined, (spc, data) =>
        {
            var meta = data.Left;
            var entityList = data.Right;
            Generate(spc, meta, entityList);
        });
    }

    private static bool IsPostgres(ProjectMeta meta)
        => meta.DbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);

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
        => type.Name == "Guid" &&
           type.ContainingNamespace is { Name: "System" };

    private static string GetDisplayTypeName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    private static PropKind ClassifyProp(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nts && nts.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return ClassifyProp(nts.TypeArguments[0]);

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_DateTime:
                return PropKind.Simple;
        }

        if (type.Name is "Guid" or "DateTimeOffset" or "TimeSpan" &&
            type.ContainingNamespace is { Name: "System" })
            return PropKind.Simple;

        if (type.TypeKind == TypeKind.Enum)
            return PropKind.Enum;

        if (type is IArrayTypeSymbol ats)
        {
            var ek = ClassifyProp(ats.ElementType);
            return ek is PropKind.Simple or PropKind.Enum ? PropKind.PrimitiveCollection : PropKind.Skip;
        }

        if (type is INamedTypeSymbol gts && gts.IsGenericType)
        {
            var defName = gts.OriginalDefinition.Name;
            if (defName == "List" || defName == "IList")
            {
                var ek = ClassifyProp(gts.TypeArguments[0]);
                return ek is PropKind.Simple or PropKind.Enum ? PropKind.PrimitiveCollection : PropKind.Skip;
            }
            if (defName == "Dictionary" || defName == "IDictionary")
                return PropKind.Dictionary;
        }

        // NetTopologySuite geometry types (Point, LineString, Polygon, MultiPoint, etc.)
        if (type.ContainingNamespace?.ToDisplayString() == "NetTopologySuite.Geometries")
            return PropKind.Geometry;

        if (type is INamedTypeSymbol candidate && candidate.TypeKind == TypeKind.Class)
        {
            if (candidate.DeclaringSyntaxReferences.Length > 0)
            {
                var hasId = candidate.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Any(p => p.Name == "Id" && IsGuidType(p.Type));
                if (!hasId)
                    return PropKind.OwnedType;
            }
        }

        return PropKind.Skip;
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

        var isPostgres = IsPostgres(meta);
        var hasGeometry = unique.Any(e => e.Properties.Any(p => p.Kind == PropKind.Geometry));

        var sb = new StringBuilder(8192);
        WriteHeader(sb);
        WriteUsings(sb, rootNs, entityNamespaces, hasGeometry);
        sb.AppendLine($"namespace {rootNs};");
        sb.AppendLine();
        WriteDbContext(sb, contextName, unique, isPostgres);
        WriteRepository(sb, contextName);
        WritePersistenceSetup(sb, contextName, rootNs, shortName, isPostgres);

        spc.AddSource("PersistenceSetup.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ─── Code Emitters ───

    private static void WriteHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// EF Core persistence generated by TheWatch.Generators.DbContextGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
    }

    private static void WriteUsings(StringBuilder sb, string rootNs, List<string> entityNs, bool hasGeometry)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        if (hasGeometry)
            sb.AppendLine("using NetTopologySuite.Geometries;");
        foreach (var ns in entityNs.Where(n => n != rootNs).OrderBy(n => n))
            sb.AppendLine($"using {ns};");
        sb.AppendLine();
    }

    private static void WriteDbContext(StringBuilder sb, string ctxName, List<EntityInfo> entities, bool isPostgres)
    {
        sb.AppendLine($"public partial class {ctxName} : DbContext");
        sb.AppendLine("{");
        sb.AppendLine($"    public {ctxName}(DbContextOptions<{ctxName}> options) : base(options) {{ }}");
        sb.AppendLine();

        foreach (var e in entities)
        {
            var plural = Pluralize(e.Name);
            sb.AppendLine($"    public DbSet<{e.Name}> {plural} => Set<{e.Name}>();");
        }
        sb.AppendLine();

        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");

        if (isPostgres)
            sb.AppendLine("        modelBuilder.HasPostgresExtension(\"postgis\");");

        foreach (var e in entities)
        {
            sb.AppendLine($"        modelBuilder.Entity<{e.Name}>(entity =>");
            sb.AppendLine("        {");
            sb.AppendLine("            entity.HasKey(e => e.Id);");
            sb.AppendLine("            entity.Property(e => e.Id).ValueGeneratedNever();");

            foreach (var p in e.Properties)
            {
                if (p.Name == "Id") continue;
                var bt = p.TypeName.TrimEnd('?');

                switch (p.Kind)
                {
                    case PropKind.Simple:
                        if (bt is "string" or "String")
                            sb.AppendLine($"            entity.Property(e => e.{p.Name}).HasMaxLength({MaxLen(p.Name)});");
                        if (p.Name.EndsWith("At") && bt.Contains("DateTime"))
                            sb.AppendLine($"            entity.HasIndex(e => e.{p.Name});");
                        if (p.Name.EndsWith("Id") && (bt is "Guid" or "System.Guid"))
                            sb.AppendLine($"            entity.HasIndex(e => e.{p.Name});");
                        break;

                    case PropKind.Enum:
                        sb.AppendLine($"            entity.Property(e => e.{p.Name}).HasConversion<string>().HasMaxLength(50);");
                        sb.AppendLine($"            entity.HasIndex(e => e.{p.Name});");
                        break;

                    case PropKind.OwnedType:
                        sb.AppendLine($"            entity.OwnsOne(e => e.{p.Name});");
                        break;

                    case PropKind.Dictionary:
                        if (isPostgres)
                            sb.AppendLine($"            entity.Property(e => e.{p.Name}).HasColumnType(\"jsonb\");");
                        else
                            sb.AppendLine($"            entity.Property(e => e.{p.Name}).HasConversion(v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null), v => System.Text.Json.JsonSerializer.Deserialize<{p.TypeName}>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new()).HasColumnType(\"nvarchar(max)\");");
                        break;

                    case PropKind.Geometry:
                        sb.AppendLine($"            entity.Property(e => e.{p.Name}).HasColumnType(\"{MapGeometryColumnType(bt)}\");");
                        sb.AppendLine($"            entity.HasIndex(e => e.{p.Name}).HasMethod(\"gist\");");
                        break;

                    case PropKind.PrimitiveCollection:
                        // EF Core 8+ handles List<string>, List<Guid>, string[] natively
                        break;
                }
            }

            sb.AppendLine("        });");
            sb.AppendLine();
        }

        sb.AppendLine($"        modelBuilder.ApplyConfigurationsFromAssembly(typeof({ctxName}).Assembly);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static string MapGeometryColumnType(string typeName)
    {
        switch (typeName)
        {
            case "Point": return "geometry (Point, 4326)";
            case "LineString": return "geometry (LineString, 4326)";
            case "Polygon": return "geometry (Polygon, 4326)";
            case "MultiPoint": return "geometry (MultiPoint, 4326)";
            case "MultiLineString": return "geometry (MultiLineString, 4326)";
            case "MultiPolygon": return "geometry (MultiPolygon, 4326)";
            case "GeometryCollection": return "geometry (GeometryCollection, 4326)";
            default: return "geometry (Geometry, 4326)";
        }
    }

    private static void WriteRepository(StringBuilder sb, string ctxName)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generic repository interface for EF-backed data access.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IWatchRepository<T> where T : class");
        sb.AppendLine("{");
        sb.AppendLine("    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);");
        sb.AppendLine("    Task<List<T>> GetAllAsync(CancellationToken ct = default);");
        sb.AppendLine("    Task<T> AddAsync(T entity, CancellationToken ct = default);");
        sb.AppendLine("    Task UpdateAsync(T entity, CancellationToken ct = default);");
        sb.AppendLine("    Task DeleteAsync(Guid id, CancellationToken ct = default);");
        sb.AppendLine("    IQueryable<T> Query();");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// EF Core implementation of IWatchRepository backed by {ctxName}.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class EfRepository<T> : IWatchRepository<T> where T : class");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {ctxName} _context;");
        sb.AppendLine();
        sb.AppendLine($"    public EfRepository({ctxName} context) => _context = context;");
        sb.AppendLine();
        sb.AppendLine("    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)");
        sb.AppendLine("        => await _context.Set<T>().FindAsync(new object[] { id }, ct);");
        sb.AppendLine();
        sb.AppendLine("    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)");
        sb.AppendLine("        => await _context.Set<T>().ToListAsync(ct);");
        sb.AppendLine();
        sb.AppendLine("    public async Task<T> AddAsync(T entity, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        _context.Set<T>().Add(entity);");
        sb.AppendLine("        await _context.SaveChangesAsync(ct);");
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task UpdateAsync(T entity, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        _context.Set<T>().Update(entity);");
        sb.AppendLine("        await _context.SaveChangesAsync(ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task DeleteAsync(Guid id, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = await GetByIdAsync(id, ct);");
        sb.AppendLine("        if (entity is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            _context.Set<T>().Remove(entity);");
        sb.AppendLine("            await _context.SaveChangesAsync(ct);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public IQueryable<T> Query() => _context.Set<T>().AsQueryable();");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WritePersistenceSetup(StringBuilder sb, string ctxName, string rootNs, string shortName, bool isPostgres)
    {
        var providerLabel = isPostgres ? "PostgreSQL + PostGIS" : "SQL Server";
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Auto-generated persistence setup for {rootNs} ({providerLabel}).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class PersistenceSetup");
        sb.AppendLine("{");
        sb.AppendLine($"    public const string DatabaseName = \"Watch{shortName}DB\";");
        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Registers EF Core DbContext + generic repository with a {providerLabel} connection string.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IServiceCollection AddWatchPersistence(");
        sb.AppendLine("        this IServiceCollection services, string connectionString)");
        sb.AppendLine("    {");
        sb.AppendLine($"        services.AddDbContext<{ctxName}>(options =>");
        if (isPostgres)
            sb.AppendLine("            options.UseNpgsql(connectionString, o => { o.UseNetTopologySuite(); o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null); }));");
        else
            sb.AppendLine("            options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)));");
        sb.AppendLine($"        services.AddScoped(typeof(IWatchRepository<>), typeof(EfRepository<>));");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers EF Core DbContext + generic repository with custom options (for testing).");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IServiceCollection AddWatchPersistence(");
        sb.AppendLine("        this IServiceCollection services, Action<DbContextOptionsBuilder> configureOptions)");
        sb.AppendLine("    {");
        sb.AppendLine($"        services.AddDbContext<{ctxName}>(configureOptions);");
        sb.AppendLine($"        services.AddScoped(typeof(IWatchRepository<>), typeof(EfRepository<>));");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Registers EF Core DbContext + generic repository via .NET Aspire service discovery.");
        sb.AppendLine($"    /// Uses the Aspire {providerLabel} integration to resolve connection strings automatically.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static WebApplicationBuilder AddWatchPersistenceAspire(this WebApplicationBuilder builder)");
        sb.AppendLine("    {");
        if (isPostgres)
        {
            sb.AppendLine($"        builder.AddNpgsqlDbContext<{ctxName}>(DatabaseName,");
            sb.AppendLine("            configureDbContextOptions: options => options.UseNpgsql(o => { o.UseNetTopologySuite(); o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null); }));");
        }
        else
        {
            sb.AppendLine($"        builder.AddSqlServerDbContext<{ctxName}>(DatabaseName,");
            sb.AppendLine("            configureDbContextOptions: options => options.UseSqlServer(o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)));");
        }
        sb.AppendLine($"        builder.Services.AddScoped(typeof(IWatchRepository<>), typeof(EfRepository<>));");
        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        sb.AppendLine();

        WriteDesignTimeFactory(sb, ctxName, rootNs, shortName, isPostgres);
    }

    private static void WriteDesignTimeFactory(StringBuilder sb, string ctxName, string rootNs, string shortName, bool isPostgres)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Design-time factory for {ctxName} — used by dotnet ef migrations tooling.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {ctxName}DesignTimeFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<{ctxName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {ctxName} CreateDbContext(string[] args)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var optionsBuilder = new DbContextOptionsBuilder<{ctxName}>();");
        if (isPostgres)
        {
            sb.AppendLine("        var conn = Environment.GetEnvironmentVariable(\"WATCH_POSTGRES_CONN\")");
            sb.AppendLine($"            ?? \"Host=localhost;Database=Watch{shortName}DB;Username=postgres;Password=postgres\";");
            sb.AppendLine("        optionsBuilder.UseNpgsql(conn, o => { o.UseNetTopologySuite(); o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null); });");
        }
        else
        {
            sb.AppendLine("        var conn = Environment.GetEnvironmentVariable(\"WATCH_SQL_CONN\")");
            sb.AppendLine($"            ?? \"Server=localhost;Database=Watch{shortName}DB;Trusted_Connection=true;TrustServerCertificate=true\";");
            sb.AppendLine("        optionsBuilder.UseSqlServer(conn, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));");
        }
        sb.AppendLine("        return new " + ctxName + "(optionsBuilder.Options);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    // ─── Helpers ───

    private static string Pluralize(string name)
    {
        if (name.EndsWith("s", StringComparison.Ordinal) ||
            name.EndsWith("x", StringComparison.Ordinal) ||
            name.EndsWith("z", StringComparison.Ordinal) ||
            name.EndsWith("ch", StringComparison.Ordinal) ||
            name.EndsWith("sh", StringComparison.Ordinal))
            return name + "es";
        if (name.EndsWith("y", StringComparison.Ordinal) && name.Length > 1 &&
            !"aeiouAEIOU".Contains(name[name.Length - 2]))
            return name.Substring(0, name.Length - 1) + "ies";
        return name + "s";
    }

    private static int MaxLen(string propName)
    {
        var lower = propName.ToLowerInvariant();
        if (lower.Contains("email")) return 256;
        if (lower.Contains("phone")) return 50;
        if (lower.Contains("name") || lower.Contains("title")) return 200;
        if (lower.Contains("url") || lower.Contains("path")) return 2048;
        if (lower.Contains("description") || lower.Contains("content") ||
            lower.Contains("notes") || lower.Contains("message")) return 4000;
        if (lower.Contains("hash") || lower.Contains("token")) return 512;
        if (lower.Contains("key")) return 256;
        if (lower.Contains("badge") || lower.Contains("unit") || lower.Contains("model") ||
            lower.Contains("version") || lower.Contains("reason") || lower.Contains("error")) return 200;
        return 500;
    }

    // ─── Pipeline Data Types (must be equatable for incremental caching) ───

    private struct ProjectMeta : IEquatable<ProjectMeta>
    {
        public string Namespace;
        public string UseMaui;
        public string DbProvider;
        public ProjectMeta(string ns, string useMaui, string dbProvider) { Namespace = ns; UseMaui = useMaui; DbProvider = dbProvider; }
        public bool Equals(ProjectMeta other) => Namespace == other.Namespace && UseMaui == other.UseMaui && DbProvider == other.DbProvider;
        public override bool Equals(object? obj) => obj is ProjectMeta o && Equals(o);
        public override int GetHashCode() => (Namespace?.GetHashCode() ?? 0) ^ (UseMaui?.GetHashCode() ?? 0) ^ (DbProvider?.GetHashCode() ?? 0);
    }

    private struct EntityInfo : IEquatable<EntityInfo>
    {
        public string Name;
        public string Namespace;
        public PropData[] Properties;

        public EntityInfo(string name, string ns, PropData[] props)
        {
            Name = name; Namespace = ns; Properties = props;
        }

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
            if (Properties != null)
                foreach (var p in Properties) h = h * 31 + p.GetHashCode();
            return h;
        }
    }

    private struct PropData : IEquatable<PropData>
    {
        public string Name;
        public string TypeName;
        public PropKind Kind;

        public PropData(string name, string typeName, PropKind kind)
        {
            Name = name; TypeName = typeName; Kind = kind;
        }

        public bool Equals(PropData other)
            => Name == other.Name && TypeName == other.TypeName && Kind == other.Kind;
        public override bool Equals(object? obj) => obj is PropData o && Equals(o);
        public override int GetHashCode()
            => (Name?.GetHashCode() ?? 0) ^ (TypeName?.GetHashCode() ?? 0) ^ (int)Kind;
    }

    private enum PropKind
    {
        Simple,
        Enum,
        OwnedType,
        PrimitiveCollection,
        Dictionary,
        Geometry,
        Skip
    }
}
