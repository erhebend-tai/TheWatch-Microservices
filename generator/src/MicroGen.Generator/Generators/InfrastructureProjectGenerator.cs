using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Infrastructure layer: EF Core DbContext, repositories, caching, telemetry, logging.
/// </summary>
public sealed class InfrastructureProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public InfrastructureProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{service.PascalName}.Infrastructure";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // DI extension
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "DependencyInjection.cs",
            _engine.Render(Templates.DependencyInjection, new { Service = service, Config = _config }),
            ct);

        // DbContext
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Persistence", $"{service.PascalName}DbContext.cs"),
            _engine.Render(Templates.DbContext, new { Service = service }),
            ct);

        // Generic repository
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Persistence", "Repository.cs"),
            _engine.Render(Templates.Repository, new { Service = service }),
            ct);

        // Unit of work
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Persistence", "UnitOfWork.cs"),
            _engine.Render(Templates.UnitOfWork, new { Service = service }),
            ct);

        // Entity configurations
        var entities = service.Schemas.Where(s => s.IsEntity).ToList();
        _logger.LogDebug("    Generating {Count} Entity Configurations...", entities.Count);

        foreach (var schema in entities)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Persistence", "Configurations", $"{schema.PascalName}Configuration.cs"),
                _engine.Render(Templates.EntityConfiguration, new { Service = service, Schema = schema }),
                ct);
        }

        // Caching
        if (_config.Features.Redis)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Caching", "RedisCacheService.cs"),
                _engine.Render(Templates.RedisCacheService, new { Service = service }),
                ct);

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Caching", "ICacheService.cs"),
                _engine.Render(Templates.ICacheService, new { Service = service }),
                ct);
        }

        // OpenTelemetry
        if (_config.Features.OpenTelemetry)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Telemetry", "TelemetryConfiguration.cs"),
                _engine.Render(Templates.TelemetryConfig, new { Service = service, Config = _config }),
                ct);
        }

        // Serilog sink configuration
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Logging", "SerilogConfiguration.cs"),
            _engine.Render(Templates.SerilogConfig, new { Service = service, Config = _config }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <RootNamespace>{{ Service.PascalName }}.Infrastructure</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*-*" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*-*" PrivateAssets="all" />
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*-*" />
            {{~ else if Config.Database.Provider == "PostgreSQL" ~}}
                <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*-*" />
            {{~ end ~}}
            {{~ if Config.Features.Redis ~}}
                <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.*" />
                <PackageReference Include="StackExchange.Redis" Version="2.*" />
            {{~ end ~}}
            {{~ if Config.Features.OpenTelemetry ~}}
                <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*-*" />
                <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
            {{~ end ~}}
                <PackageReference Include="Polly" Version="8.*" />
                <PackageReference Include="Polly.Extensions.Http" Version="3.*" />
                <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
                <PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
                <PackageReference Include="Serilog.Sinks.File" Version="6.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Domain\{{ Service.PascalName }}.Domain.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Application\{{ Service.PascalName }}.Application.csproj" />
              </ItemGroup>
            </Project>
            """;

        public const string DependencyInjection = """
            using {{ Service.PascalName }}.Application.Interfaces;
            using {{ Service.PascalName }}.Infrastructure.Persistence;
            {{~ if Config.Features.Redis ~}}
            using {{ Service.PascalName }}.Infrastructure.Caching;
            {{~ end ~}}
            {{~ if Config.Features.OpenTelemetry ~}}
            using {{ Service.PascalName }}.Infrastructure.Telemetry;
            {{~ end ~}}
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{ Service.PascalName }}.Infrastructure;

            public static class DependencyInjection
            {
                public static IServiceCollection AddInfrastructureServices(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
                    // EF Core
                    services.AddDbContext<{{ Service.PascalName }}DbContext>(options =>
                    {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                        options.UseSqlServer(
                            configuration.GetConnectionString("DefaultConnection"),
                            b => b.MigrationsAssembly(typeof({{ Service.PascalName }}DbContext).Assembly.FullName));
            {{~ else if Config.Database.Provider == "PostgreSQL" ~}}
                        options.UseNpgsql(
                            configuration.GetConnectionString("DefaultConnection"),
                            b => b.MigrationsAssembly(typeof({{ Service.PascalName }}DbContext).Assembly.FullName));
            {{~ end ~}}
                    });

                    // Repositories
                    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                    services.AddScoped<IUnitOfWork, UnitOfWork>();

            {{~ if Config.Features.Redis ~}}
                    // Redis caching
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = configuration.GetConnectionString("Redis");
                        options.InstanceName = "{{ Service.PascalName }}:";
                    });
                    services.AddSingleton<ICacheService, RedisCacheService>();
            {{~ end ~}}

            {{~ if Config.Features.OpenTelemetry ~}}
                    // OpenTelemetry
                    services.AddTelemetry(configuration);
            {{~ end ~}}

                    return services;
                }
            }
            """;

        public const string DbContext = """
            using {{ Service.PascalName }}.Domain.Common;
            using {{ Service.PascalName }}.Domain.Entities;
            using MediatR;
            using Microsoft.EntityFrameworkCore;

            namespace {{ Service.PascalName }}.Infrastructure.Persistence;

            public class {{ Service.PascalName }}DbContext : DbContext
            {
                private readonly IMediator _mediator;

                public {{ Service.PascalName }}DbContext(
                    DbContextOptions<{{ Service.PascalName }}DbContext> options,
                    IMediator mediator)
                    : base(options)
                {
                    _mediator = mediator;
                }

            {{~ for schema in Service.Schemas ~}}
            {{~ if schema.IsEntity ~}}
                public DbSet<{{ schema.PascalName }}> {{ schema.PascalName }}s { get; set; } = null!;
            {{~ end ~}}
            {{~ end ~}}

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.ApplyConfigurationsFromAssembly(typeof({{ Service.PascalName }}DbContext).Assembly);
                    base.OnModelCreating(modelBuilder);
                }

                public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                {
                    // Audit fields
                    foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
                    {
                        switch (entry.State)
                        {
                            case EntityState.Added:
                                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                                break;
                            case EntityState.Modified:
                                entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
                                break;
                        }
                    }

                    // Dispatch domain events
                    var entities = ChangeTracker.Entries<BaseEntity>()
                        .Where(e => e.Entity.DomainEvents.Count > 0)
                        .Select(e => e.Entity)
                        .ToList();

                    var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();
                    entities.ForEach(e => e.ClearDomainEvents());

                    var result = await base.SaveChangesAsync(cancellationToken);

                    foreach (var domainEvent in domainEvents)
                    {
                        await _mediator.Publish(domainEvent, cancellationToken);
                    }

                    return result;
                }
            }
            """;

        public const string Repository = """
            using System.Linq.Expressions;
            using {{ Service.PascalName }}.Application.Interfaces;
            using Microsoft.EntityFrameworkCore;

            namespace {{ Service.PascalName }}.Infrastructure.Persistence;

            public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
            {
                protected readonly {{ Service.PascalName }}DbContext _context;
                protected readonly DbSet<TEntity> _dbSet;

                public Repository({{ Service.PascalName }}DbContext context)
                {
                    _context = context;
                    _dbSet = context.Set<TEntity>();
                }

                public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                    => await _dbSet.FindAsync([id], cancellationToken);

                public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
                    => await _dbSet.ToListAsync(cancellationToken);

                public async Task<IReadOnlyList<TEntity>> FindAsync(
                    Expression<Func<TEntity, bool>> predicate,
                    CancellationToken cancellationToken = default)
                    => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

                public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    await _dbSet.AddAsync(entity, cancellationToken);
                    return entity;
                }

                public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    _dbSet.Update(entity);
                    return Task.CompletedTask;
                }

                public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    _dbSet.Remove(entity);
                    return Task.CompletedTask;
                }

                public async Task<int> CountAsync(CancellationToken cancellationToken = default)
                    => await _dbSet.CountAsync(cancellationToken);

                public async Task<bool> ExistsAsync(
                    Expression<Func<TEntity, bool>> predicate,
                    CancellationToken cancellationToken = default)
                    => await _dbSet.AnyAsync(predicate, cancellationToken);
            }
            """;

        public const string UnitOfWork = """
            using {{ Service.PascalName }}.Application.Interfaces;
            using Microsoft.EntityFrameworkCore.Storage;

            namespace {{ Service.PascalName }}.Infrastructure.Persistence;

            public class UnitOfWork : IUnitOfWork
            {
                private readonly {{ Service.PascalName }}DbContext _context;
                private IDbContextTransaction? _transaction;

                public UnitOfWork({{ Service.PascalName }}DbContext context) => _context = context;

                public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                    => await _context.SaveChangesAsync(cancellationToken);

                public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
                    => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
                {
                    if (_transaction is not null)
                    {
                        await _transaction.CommitAsync(cancellationToken);
                        await _transaction.DisposeAsync();
                        _transaction = null;
                    }
                }

                public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
                {
                    if (_transaction is not null)
                    {
                        await _transaction.RollbackAsync(cancellationToken);
                        await _transaction.DisposeAsync();
                        _transaction = null;
                    }
                }

                public void Dispose()
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
            """;

        public const string EntityConfiguration = """
            using {{ Service.PascalName }}.Domain.Entities;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace {{ Service.PascalName }}.Infrastructure.Persistence.Configurations;

            public class {{ Schema.PascalName }}Configuration : IEntityTypeConfiguration<{{ Schema.PascalName }}>
            {
                public void Configure(EntityTypeBuilder<{{ Schema.PascalName }}> builder)
                {
                    builder.ToTable("{{ Schema.PascalName }}s");
                    builder.HasKey(e => e.Id);
                    builder.Property(e => e.Id).ValueGeneratedOnAdd();
                    builder.Property(e => e.CreatedAt).IsRequired();

            {{~ for prop in Schema.Properties ~}}
            {{~ if prop.Required && prop.Name != "id" && prop.Name != "createdAt" ~}}
                    builder.Property(e => e.{{ prop.PascalName }}).IsRequired(){{ if prop.MaxLength > 0 }}.HasMaxLength({{ prop.MaxLength }}){{ end }};
            {{~ end ~}}
            {{~ end ~}}
                }
            }
            """;

        public const string ICacheService = """
            namespace {{ Service.PascalName }}.Infrastructure.Caching;

            public interface ICacheService
            {
                Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
                Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
                Task RemoveAsync(string key, CancellationToken cancellationToken = default);
                Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
            }
            """;

        public const string RedisCacheService = """
            using System.Text.Json;
            using Microsoft.Extensions.Caching.Distributed;

            namespace {{ Service.PascalName }}.Infrastructure.Caching;

            public class RedisCacheService : ICacheService
            {
                private readonly IDistributedCache _cache;
                private static readonly JsonSerializerOptions JsonOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                public RedisCacheService(IDistributedCache cache) => _cache = cache;

                public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
                {
                    var data = await _cache.GetStringAsync(key, cancellationToken);
                    return data is null ? default : JsonSerializer.Deserialize<T>(data, JsonOptions);
                }

                public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
                {
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
                    };
                    var json = JsonSerializer.Serialize(value, JsonOptions);
                    await _cache.SetStringAsync(key, json, options, cancellationToken);
                }

                public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
                    => await _cache.RemoveAsync(key, cancellationToken);

                public async Task<T> GetOrSetAsync<T>(
                    string key,
                    Func<Task<T>> factory,
                    TimeSpan? expiry = null,
                    CancellationToken cancellationToken = default)
                {
                    var cached = await GetAsync<T>(key, cancellationToken);
                    if (cached is not null)
                        return cached;

                    var value = await factory();
                    await SetAsync(key, value, expiry, cancellationToken);
                    return value;
                }
            }
            """;

        public const string TelemetryConfig = """
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using OpenTelemetry.Metrics;
            using OpenTelemetry.Resources;
            using OpenTelemetry.Trace;

            namespace {{ Service.PascalName }}.Infrastructure.Telemetry;

            public static class TelemetryConfiguration
            {
                public static IServiceCollection AddTelemetry(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
                    var serviceName = "{{ Service.PascalName }}";
                    var serviceVersion = "{{ Service.Version }}";

                    services.AddOpenTelemetry()
                        .ConfigureResource(r => r
                            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                        .WithTracing(builder => builder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddConsoleExporter()
                            .AddOtlpExporter())
                        .WithMetrics(builder => builder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddRuntimeInstrumentation()
                            .AddProcessInstrumentation()
                            .AddConsoleExporter()
                            .AddPrometheusExporter()
                            .AddOtlpExporter());

                    return services;
                }
            }
            """;

        public const string SerilogConfig = """
            using Microsoft.Extensions.Configuration;
            using Serilog;

            namespace {{ Service.PascalName }}.Infrastructure.Logging;

            public static class SerilogConfiguration
            {
                public static LoggerConfiguration CreateDefaultConfiguration(IConfiguration configuration)
                {
                    return new LoggerConfiguration()
                        .ReadFrom.Configuration(configuration)
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithProperty("ServiceName", "{{ Service.PascalName }}")
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
                }
            }
            """;
    }
}
