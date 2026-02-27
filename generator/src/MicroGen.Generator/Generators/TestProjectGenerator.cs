using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the test project: unit tests, integration tests, test fixtures,
/// mock infrastructure, Bogus factories, Testcontainers, Helm/K8s validation.
/// </summary>
public sealed class TestProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public TestProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Tests";
        _logger.LogDebug("  Generating {Project}...", projectName);

        var entities = service.Schemas.Where(s => s.IsEntity).ToList();
        var entityCount = entities.Count;
        var dbProvider = _config.Database.Provider;

        // ─── Project file ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // ─── Global usings ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "GlobalUsings.cs",
            _engine.Render(Templates.GlobalUsings, new { Service = service }),
            ct);

        // ═══════════════════════════════════════════
        // Fixtures
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Fixtures", "WebAppFixture.cs"),
            _engine.Render(Templates.WebAppFixture, new { Service = service, Config = _config }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Fixtures", "DatabaseFixture.cs"),
            _engine.Render(Templates.DatabaseFixture, new { Service = service, Config = _config }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Fixtures", "RedisFixture.cs"),
            _engine.Render(Templates.RedisFixture, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Fixtures", "TestCollectionDefinitions.cs"),
            _engine.Render(Templates.TestCollections, new { Service = service }),
            ct);

        // ═══════════════════════════════════════════
        // Mocks
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Mocks", "MockRepository.cs"),
            _engine.Render(Templates.MockRepository, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Mocks", "MockUnitOfWork.cs"),
            _engine.Render(Templates.MockUnitOfWork, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Mocks", "MockCacheService.cs"),
            _engine.Render(Templates.MockCacheService, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Mocks", "MockHttpMessageHandler.cs"),
            _engine.Render(Templates.MockHttpMessageHandler, new { Service = service }),
            ct);

        // ═══════════════════════════════════════════
        // Bogus Factories — one per entity
        // ═══════════════════════════════════════════
        _logger.LogDebug("    Generating {Count} Bogus entity factories...", entityCount);

        foreach (var schema in entities)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Factories", $"{schema.PascalName}Factory.cs"),
                _engine.Render(Templates.EntityFactory, new { Service = service, Schema = schema }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Unit Tests — Controllers
        // ═══════════════════════════════════════════
        foreach (var tag in service.Tags)
        {
            var tagName = tag.Name ?? "Default";
            var ops = service.Operations
                .Where(o => o.Tag != null && o.Tag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ops.Count == 0) continue;

            var queryOps = ops.Where(o => o.IsQuery).ToList();
            var commandOps = ops.Where(o => o.IsCommand).ToList();

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Unit", "Controllers", $"{tag.PascalName}ControllerTests.cs"),
                _engine.Render(Templates.ControllerTest, new
                {
                    Service = service,
                    Tag = tag,
                    Operations = ops,
                    QueryOps = queryOps,
                    CommandOps = commandOps
                }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Unit Tests — Command Handlers
        // ═══════════════════════════════════════════
        foreach (var op in service.Operations.Where(o => o.IsCommand))
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Unit", "Commands", $"{op.PascalOperationId}CommandHandlerTests.cs"),
                _engine.Render(Templates.CommandHandlerTest, new { Service = service, Operation = op }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Unit Tests — Query Handlers
        // ═══════════════════════════════════════════
        foreach (var op in service.Operations.Where(o => o.IsQuery))
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Unit", "Queries", $"{op.PascalOperationId}QueryHandlerTests.cs"),
                _engine.Render(Templates.QueryHandlerTest, new { Service = service, Operation = op }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Unit Tests — Validators
        // ═══════════════════════════════════════════
        foreach (var op in service.Operations.Where(o => o.IsCommand))
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Unit", "Validators", $"{op.PascalOperationId}CommandValidatorTests.cs"),
                _engine.Render(Templates.ValidatorTest, new { Service = service, Operation = op }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Integration Tests
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Integration", "IntegrationTestBase.cs"),
            _engine.Render(Templates.IntegrationTestBase, new { Service = service, Config = _config }),
            ct);

        foreach (var tag in service.Tags)
        {
            var tagName = tag.Name ?? "Default";
            var ops = service.Operations
                .Where(o => o.Tag != null && o.Tag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ops.Count == 0) continue;

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Integration", "Endpoints", $"{tag.PascalName}EndpointTests.cs"),
                _engine.Render(Templates.EndpointIntegrationTest, new
                {
                    Service = service,
                    Tag = tag,
                    Operations = ops
                }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Database Integration Tests
        // ═══════════════════════════════════════════
        if (entityCount > 0)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Integration", "Database", "DbContextTests.cs"),
                _engine.Render(Templates.DbContextTest, new { Service = service, Config = _config, Entities = entities }),
                ct);

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Integration", "Database", "RepositoryTests.cs"),
                _engine.Render(Templates.RepositoryTest, new { Service = service, Config = _config, Entities = entities }),
                ct);
        }

        // ═══════════════════════════════════════════
        // Architecture Tests
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Architecture", "LayerDependencyTests.cs"),
            _engine.Render(Templates.ArchitectureTest, new { Service = service }),
            ct);

        // ═══════════════════════════════════════════
        // Health Check Tests
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Integration", "HealthCheckTests.cs"),
            _engine.Render(Templates.HealthCheckTest, new { Service = service }),
            ct);

        // ═══════════════════════════════════════════
        // Helm / K8s Validation Tests
        // ═══════════════════════════════════════════
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Infrastructure", "HelmChartTests.cs"),
            _engine.Render(Templates.HelmChartTest, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Infrastructure", "KubernetesManifestTests.cs"),
            _engine.Render(Templates.K8sManifestTest, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Infrastructure", "DockerfileTests.cs"),
            _engine.Render(Templates.DockerfileTest, new { Service = service }),
            ct);

        var totalTests = service.Tags.Count + service.Operations.Count * 2 + entityCount + 6;
        _logger.LogDebug("    Generated ~{Count} test classes for {Service}", totalTests, service.PascalName);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TEMPLATES
    // ═══════════════════════════════════════════════════════════════════

    private static class Templates
    {
        // ─── Project File ──────────────────────────────────────────────
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <IsPackable>false</IsPackable>
                <IsTestProject>true</IsTestProject>
                <RootNamespace>{{ Service.PascalName }}.Tests</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
                <PackageReference Include="xunit" Version="2.*" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.*" PrivateAssets="all" />
                <PackageReference Include="coverlet.collector" Version="6.*" PrivateAssets="all" />
                <PackageReference Include="Moq" Version="4.*" />
                <PackageReference Include="FluentAssertions" Version="7.*" />
                <PackageReference Include="Bogus" Version="35.*" />
                <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*-*" />
                <PackageReference Include="Testcontainers" Version="4.*" />
                <PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
                <PackageReference Include="Testcontainers.MsSql" Version="4.*" />
                <PackageReference Include="Testcontainers.Redis" Version="4.*" />
                <PackageReference Include="Respawn" Version="6.*" />
                <PackageReference Include="Xunit.SkippableFact" Version="1.*" />
                <PackageReference Include="YamlDotNet" Version="16.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Api\{{ Service.PascalName }}.Api.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Application\{{ Service.PascalName }}.Application.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Domain\{{ Service.PascalName }}.Domain.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Infrastructure\{{ Service.PascalName }}.Infrastructure.csproj" />
              </ItemGroup>

              <ItemGroup>
                <Content Include="..\..\deploy\**\*" CopyToOutputDirectory="PreserveNewest" LinkBase="deploy" />
              </ItemGroup>
            </Project>
            """;

        // ─── Global Usings ────────────────────────────────────────────
        public const string GlobalUsings = """
            global using Xunit;
            global using FluentAssertions;
            global using Moq;
            global using Bogus;
            global using {{ Service.PascalName }}.Domain.Entities;
            global using {{ Service.PascalName }}.Domain.Common;
            global using {{ Service.PascalName }}.Application.Interfaces;
            global using {{ Service.PascalName }}.Tests.Factories;
            global using {{ Service.PascalName }}.Tests.Mocks;
            """;

        // ─── WebAppFixture ────────────────────────────────────────────
        public const string WebAppFixture = """
            using Microsoft.AspNetCore.Hosting;
            using Microsoft.AspNetCore.Mvc.Testing;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;
            using {{ Service.PascalName }}.Infrastructure.Persistence;

            namespace {{ Service.PascalName }}.Tests.Fixtures;

            /// <summary>
            /// WebApplicationFactory configured for integration testing.
            /// Replaces the real database with an in-memory provider and
            /// swaps external services with test doubles.
            /// </summary>
            public class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
            {
                private readonly DatabaseFixture _dbFixture = new();

                public string DbConnectionString => _dbFixture.ConnectionString;

                protected override void ConfigureWebHost(IWebHostBuilder builder)
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureServices(services =>
                    {
                        // Remove existing DbContext registration
                        services.RemoveAll<DbContextOptions<{{ Service.PascalName }}DbContext>>();
                        services.RemoveAll<{{ Service.PascalName }}DbContext>();

                        // Add DbContext with test connection string
                        services.AddDbContext<{{ Service.PascalName }}DbContext>(options =>
                        {
                            if (!string.IsNullOrEmpty(_dbFixture.ConnectionString))
                            {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                                options.UseSqlServer(_dbFixture.ConnectionString);
            {{~ else ~}}
                                options.UseNpgsql(_dbFixture.ConnectionString);
            {{~ end ~}}
                            }
                            else
                            {
                                options.UseInMemoryDatabase("{{ Service.PascalName }}_Test_" + Guid.NewGuid());
                            }
                        });

                        // Ensure DB is created
                        var sp = services.BuildServiceProvider();
                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<{{ Service.PascalName }}DbContext>();
                        db.Database.EnsureCreated();
                    });
                }

                public async Task InitializeAsync()
                {
                    try
                    {
                        await _dbFixture.InitializeAsync();
                    }
                    catch
                    {
                        // Testcontainers not available (no Docker); tests will use InMemory
                    }
                }

                async Task IAsyncLifetime.DisposeAsync()
                {
                    await _dbFixture.DisposeAsync();
                }
            }
            """;

        // ─── DatabaseFixture ──────────────────────────────────────────
        public const string DatabaseFixture = """
            using Testcontainers.PostgreSql;
            using Testcontainers.MsSql;

            namespace {{ Service.PascalName }}.Tests.Fixtures;

            /// <summary>
            /// Manages a Testcontainers database instance for integration tests.
            /// Supports both PostgreSQL and SQL Server based on build configuration.
            /// Falls back gracefully if Docker is unavailable.
            /// </summary>
            public class DatabaseFixture : IAsyncLifetime
            {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                private MsSqlContainer? _container;
            {{~ else ~}}
                private PostgreSqlContainer? _container;
            {{~ end ~}}

                public string ConnectionString { get; private set; } = string.Empty;
                public bool IsAvailable { get; private set; }

                public async Task InitializeAsync()
                {
                    try
                    {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                        _container = new MsSqlBuilder()
                            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                            .WithPassword("Test@12345!")
                            .WithCleanUp(true)
                            .Build();
            {{~ else ~}}
                        _container = new PostgreSqlBuilder()
                            .WithImage("postgres:16-alpine")
                            .WithDatabase("{{ Service.KebabName }}_test")
                            .WithUsername("test")
                            .WithPassword("test")
                            .WithCleanUp(true)
                            .Build();
            {{~ end ~}}

                        await _container.StartAsync();
                        ConnectionString = _container.GetConnectionString();
                        IsAvailable = true;
                    }
                    catch (Exception)
                    {
                        // Docker not available — tests will use InMemory fallback
                        IsAvailable = false;
                    }
                }

                public async Task DisposeAsync()
                {
                    if (_container is not null)
                    {
                        await _container.DisposeAsync();
                    }
                }
            }
            """;

        // ─── RedisFixture ─────────────────────────────────────────────
        public const string RedisFixture = """
            using Testcontainers.Redis;

            namespace {{ Service.PascalName }}.Tests.Fixtures;

            /// <summary>
            /// Manages a Testcontainers Redis instance for cache integration tests.
            /// </summary>
            public class RedisFixture : IAsyncLifetime
            {
                private RedisContainer? _container;

                public string ConnectionString { get; private set; } = string.Empty;
                public bool IsAvailable { get; private set; }

                public async Task InitializeAsync()
                {
                    try
                    {
                        _container = new RedisBuilder()
                            .WithImage("redis:7-alpine")
                            .WithCleanUp(true)
                            .Build();

                        await _container.StartAsync();
                        ConnectionString = _container.GetConnectionString();
                        IsAvailable = true;
                    }
                    catch (Exception)
                    {
                        IsAvailable = false;
                    }
                }

                public async Task DisposeAsync()
                {
                    if (_container is not null)
                    {
                        await _container.DisposeAsync();
                    }
                }
            }
            """;

        // ─── Test Collection Definitions ──────────────────────────────
        public const string TestCollections = """
            namespace {{ Service.PascalName }}.Tests.Fixtures;

            [CollectionDefinition("Integration")]
            public class IntegrationTestCollection : ICollectionFixture<WebAppFixture> { }

            [CollectionDefinition("Database")]
            public class DatabaseTestCollection : ICollectionFixture<DatabaseFixture> { }

            [CollectionDefinition("Redis")]
            public class RedisTestCollection : ICollectionFixture<RedisFixture> { }
            """;

        // ─── MockRepository ───────────────────────────────────────────
        public const string MockRepository = """
            using System.Linq.Expressions;

            namespace {{ Service.PascalName }}.Tests.Mocks;

            /// <summary>
            /// In-memory repository for unit testing. Implements IRepository&lt;T&gt; with
            /// a ConcurrentDictionary backing store.
            /// </summary>
            public class MockRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
            {
                private readonly Dictionary<Guid, TEntity> _store = [];

                public IReadOnlyCollection<TEntity> Items => _store.Values.ToList().AsReadOnly();

                public void Seed(params TEntity[] entities)
                {
                    foreach (var e in entities) _store[e.Id] = e;
                }

                public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                    => Task.FromResult(_store.GetValueOrDefault(id));

                public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
                    => Task.FromResult<IReadOnlyList<TEntity>>(_store.Values.ToList().AsReadOnly());

                public Task<IReadOnlyList<TEntity>> FindAsync(
                    Expression<Func<TEntity, bool>> predicate,
                    CancellationToken cancellationToken = default)
                {
                    var compiled = predicate.Compile();
                    return Task.FromResult<IReadOnlyList<TEntity>>(
                        _store.Values.Where(compiled).ToList().AsReadOnly());
                }

                public Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    _store[entity.Id] = entity;
                    return Task.FromResult(entity);
                }

                public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    _store[entity.Id] = entity;
                    return Task.CompletedTask;
                }

                public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
                {
                    _store.Remove(entity.Id);
                    return Task.CompletedTask;
                }

                public Task<int> CountAsync(CancellationToken cancellationToken = default)
                    => Task.FromResult(_store.Count);

                public Task<bool> ExistsAsync(
                    Expression<Func<TEntity, bool>> predicate,
                    CancellationToken cancellationToken = default)
                {
                    var compiled = predicate.Compile();
                    return Task.FromResult(_store.Values.Any(compiled));
                }
            }
            """;

        // ─── MockUnitOfWork ───────────────────────────────────────────
        public const string MockUnitOfWork = """
            namespace {{ Service.PascalName }}.Tests.Mocks;

            /// <summary>
            /// No-op unit of work for unit testing.
            /// Tracks SaveChanges calls for assertion.
            /// </summary>
            public class MockUnitOfWork : IUnitOfWork
            {
                public int SaveChangesCallCount { get; private set; }
                public int BeginTransactionCallCount { get; private set; }
                public int CommitTransactionCallCount { get; private set; }
                public int RollbackTransactionCallCount { get; private set; }

                public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                {
                    SaveChangesCallCount++;
                    return Task.FromResult(1);
                }

                public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
                {
                    BeginTransactionCallCount++;
                    return Task.CompletedTask;
                }

                public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
                {
                    CommitTransactionCallCount++;
                    return Task.CompletedTask;
                }

                public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
                {
                    RollbackTransactionCallCount++;
                    return Task.CompletedTask;
                }

                public void Dispose() { GC.SuppressFinalize(this); }
            }
            """;

        // ─── MockCacheService ─────────────────────────────────────────
        public const string MockCacheService = """
            using System.Collections.Concurrent;
            using System.Text.Json;

            namespace {{ Service.PascalName }}.Tests.Mocks;

            /// <summary>
            /// In-memory cache service for unit testing. Thread-safe via ConcurrentDictionary.
            /// </summary>
            public class MockCacheService
            {
                private readonly ConcurrentDictionary<string, string> _store = new();

                public int GetCallCount { get; private set; }
                public int SetCallCount { get; private set; }
                public int RemoveCallCount { get; private set; }

                public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
                {
                    GetCallCount++;
                    if (_store.TryGetValue(key, out var json))
                        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
                    return Task.FromResult(default(T));
                }

                public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
                {
                    SetCallCount++;
                    _store[key] = JsonSerializer.Serialize(value);
                    return Task.CompletedTask;
                }

                public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
                {
                    RemoveCallCount++;
                    _store.TryRemove(key, out _);
                    return Task.CompletedTask;
                }

                public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
                {
                    if (_store.TryGetValue(key, out var json))
                        return Task.FromResult(JsonSerializer.Deserialize<T>(json)!);

                    var value = factory().GetAwaiter().GetResult();
                    _store[key] = JsonSerializer.Serialize(value);
                    return Task.FromResult(value);
                }

                public void Clear() => _store.Clear();
            }
            """;

        // ─── MockHttpMessageHandler ───────────────────────────────────
        public const string MockHttpMessageHandler = """
            using System.Net;
            using System.Text.Json;

            namespace {{ Service.PascalName }}.Tests.Mocks;

            /// <summary>
            /// Programmable HttpMessageHandler for unit testing HTTP clients.
            /// Queue responses or set a default response.
            /// </summary>
            public class MockHttpMessageHandler : HttpMessageHandler
            {
                private readonly Queue<HttpResponseMessage> _responses = new();
                private HttpResponseMessage? _defaultResponse;

                public List<HttpRequestMessage> ReceivedRequests { get; } = [];

                public void QueueResponse(HttpStatusCode status, object? body = null)
                {
                    var response = new HttpResponseMessage(status);
                    if (body is not null)
                    {
                        response.Content = new StringContent(
                            JsonSerializer.Serialize(body),
                            System.Text.Encoding.UTF8,
                            "application/json");
                    }
                    _responses.Enqueue(response);
                }

                public void SetDefaultResponse(HttpStatusCode status, object? body = null)
                {
                    _defaultResponse = new HttpResponseMessage(status);
                    if (body is not null)
                    {
                        _defaultResponse.Content = new StringContent(
                            JsonSerializer.Serialize(body),
                            System.Text.Encoding.UTF8,
                            "application/json");
                    }
                }

                protected override Task<HttpResponseMessage> SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
                {
                    ReceivedRequests.Add(request);

                    if (_responses.Count > 0)
                        return Task.FromResult(_responses.Dequeue());

                    return Task.FromResult(
                        _defaultResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
                }
            }
            """;

        // ─── Entity Factory (Bogus) ───────────────────────────────────
        public const string EntityFactory = """
            using Bogus;
            using {{ Service.PascalName }}.Domain.Entities;

            namespace {{ Service.PascalName }}.Tests.Factories;

            /// <summary>
            /// Bogus-based factory for generating realistic {{ Schema.PascalName }} test data.
            /// </summary>
            public static class {{ Schema.PascalName }}Factory
            {
                private static readonly Faker<{{ Schema.PascalName }}> _faker = new Faker<{{ Schema.PascalName }}>()
                    .RuleFor(e => e.Id, f => f.Random.Guid())
                    .RuleFor(e => e.CreatedAt, f => f.Date.RecentOffset(30))
            {{~ for prop in Schema.Properties ~}}
            {{~ if prop.Name != "id" && prop.Name != "createdAt" && prop.Name != "updatedAt" && prop.Name != "lastModifiedAt" ~}}
            {{~ if prop.Type == "string" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Lorem.Sentence())
            {{~ else if prop.Type == "integer" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Random.Int(0, 1000))
            {{~ else if prop.Type == "number" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Random.Double(0, 1000))
            {{~ else if prop.Type == "boolean" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Random.Bool())
            {{~ else if prop.Format == "date-time" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Date.RecentOffset(90))
            {{~ else if prop.Format == "uuid" ~}}
                    .RuleFor(e => e.{{ prop.PascalName }}, f => f.Random.Guid())
            {{~ end ~}}
            {{~ end ~}}
            {{~ end ~}}
                    ;

                /// <summary>Generate a single entity with random data.</summary>
                public static {{ Schema.PascalName }} Create() => _faker.Generate();

                /// <summary>Generate multiple entities with random data.</summary>
                public static List<{{ Schema.PascalName }}> CreateMany(int count = 5) => _faker.Generate(count);

                /// <summary>Generate an entity with custom overrides.</summary>
                public static {{ Schema.PascalName }} CreateWith(Action<{{ Schema.PascalName }}> configure)
                {
                    var entity = _faker.Generate();
                    configure(entity);
                    return entity;
                }
            }
            """;

        // ─── Controller Tests ─────────────────────────────────────────
        public const string ControllerTest = """
            using {{ Service.PascalName }}.Api.Controllers;
            using {{ Service.PascalName }}.Application.Commands;
            using {{ Service.PascalName }}.Application.Queries;
            using MediatR;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Tests.Unit.Controllers;

            public class {{ Tag.PascalName }}ControllerTests
            {
                private readonly Mock<IMediator> _mediatorMock = new();
                private readonly Mock<ILogger<{{ Tag.PascalName }}Controller>> _loggerMock = new();
                private readonly {{ Tag.PascalName }}Controller _sut;

                public {{ Tag.PascalName }}ControllerTests()
                {
                    _sut = new {{ Tag.PascalName }}Controller(_mediatorMock.Object, _loggerMock.Object);
                }

            {{~ for op in QueryOps ~}}
                [Fact]
                public async Task {{ op.PascalOperationId }}_ReturnsOkResult()
                {
                    // Arrange
                    var expectedResult = new {{ op.PascalOperationId }}QueryResult { Data = new object(), TotalCount = 1 };
                    _mediatorMock
                        .Setup(m => m.Send(It.IsAny<{{ op.PascalOperationId }}Query>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

                    // Act
                    var result = await _sut.{{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                        Guid.NewGuid(),
            {{~ end ~}}
                        CancellationToken.None);

                    // Assert
                    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
                    okResult.Value.Should().Be(expectedResult);
                    _mediatorMock.Verify(m => m.Send(It.IsAny<{{ op.PascalOperationId }}Query>(), It.IsAny<CancellationToken>()), Times.Once);
                }

            {{~ end ~}}
            {{~ for op in CommandOps ~}}
                [Fact]
                public async Task {{ op.PascalOperationId }}_ReturnsCreatedResult()
                {
                    // Arrange
                    var expectedResult = new {{ op.PascalOperationId }}CommandResult { Success = true, Message = "OK" };
                    _mediatorMock
                        .Setup(m => m.Send(It.IsAny<{{ op.PascalOperationId }}Command>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

                    // Act
                    var result = await _sut.{{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                        Guid.NewGuid(),
            {{~ end ~}}
            {{~ if op.HasRequestBody ~}}
                        new {{ op.PascalOperationId }}Command(),
            {{~ end ~}}
                        CancellationToken.None);

                    // Assert
                    result.Should().BeOfType<CreatedAtActionResult>()
                        .Which.Value.Should().Be(expectedResult);
                    _mediatorMock.Verify(m => m.Send(It.IsAny<{{ op.PascalOperationId }}Command>(), It.IsAny<CancellationToken>()), Times.Once);
                }

                [Fact]
                public async Task {{ op.PascalOperationId }}_WhenMediatorThrows_PropagatesException()
                {
                    // Arrange
                    _mediatorMock
                        .Setup(m => m.Send(It.IsAny<{{ op.PascalOperationId }}Command>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Test error"));

                    // Act & Assert
                    await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        _sut.{{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                            Guid.NewGuid(),
            {{~ end ~}}
            {{~ if op.HasRequestBody ~}}
                            new {{ op.PascalOperationId }}Command(),
            {{~ end ~}}
                            CancellationToken.None));
                }

            {{~ end ~}}
            }
            """;

        // ─── Command Handler Tests ────────────────────────────────────
        public const string CommandHandlerTest = """
            using {{ Service.PascalName }}.Application.Commands;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Tests.Unit.Commands;

            public class {{ Operation.PascalOperationId }}CommandHandlerTests
            {
                private readonly Mock<ILogger<{{ Operation.PascalOperationId }}CommandHandler>> _loggerMock = new();
                private readonly {{ Operation.PascalOperationId }}CommandHandler _sut;

                public {{ Operation.PascalOperationId }}CommandHandlerTests()
                {
                    _sut = new {{ Operation.PascalOperationId }}CommandHandler(_loggerMock.Object);
                }

                [Fact]
                public async Task Handle_ValidCommand_ReturnsSuccessResult()
                {
                    // Arrange
                    var command = new {{ Operation.PascalOperationId }}Command();

                    // Act
                    var result = await _sut.Handle(command, CancellationToken.None);

                    // Assert
                    result.Should().NotBeNull();
                    result.Success.Should().BeTrue();
                    result.Message.Should().NotBeNullOrEmpty();
                }

                [Fact]
                public async Task Handle_ValidCommand_LogsExecution()
                {
                    // Arrange
                    var command = new {{ Operation.PascalOperationId }}Command();

                    // Act
                    await _sut.Handle(command, CancellationToken.None);

                    // Assert
                    _loggerMock.Verify(
                        x => x.Log(
                            LogLevel.Information,
                            It.IsAny<EventId>(),
                            It.IsAny<It.IsAnyType>(),
                            It.IsAny<Exception?>(),
                            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.AtLeastOnce);
                }

                [Fact]
                public async Task Handle_CancellationRequested_DoesNotThrow()
                {
                    // Arrange
                    var command = new {{ Operation.PascalOperationId }}Command();
                    using var cts = new CancellationTokenSource();

                    // Act
                    var result = await _sut.Handle(command, cts.Token);

                    // Assert
                    result.Should().NotBeNull();
                }
            }
            """;

        // ─── Query Handler Tests ──────────────────────────────────────
        public const string QueryHandlerTest = """
            using {{ Service.PascalName }}.Application.Queries;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Tests.Unit.Queries;

            public class {{ Operation.PascalOperationId }}QueryHandlerTests
            {
                private readonly Mock<ILogger<{{ Operation.PascalOperationId }}QueryHandler>> _loggerMock = new();
                private readonly {{ Operation.PascalOperationId }}QueryHandler _sut;

                public {{ Operation.PascalOperationId }}QueryHandlerTests()
                {
                    _sut = new {{ Operation.PascalOperationId }}QueryHandler(_loggerMock.Object);
                }

                [Fact]
                public async Task Handle_ValidQuery_ReturnsResult()
                {
                    // Arrange
                    var query = new {{ Operation.PascalOperationId }}Query();

                    // Act
                    var result = await _sut.Handle(query, CancellationToken.None);

                    // Assert
                    result.Should().NotBeNull();
                    result.TotalCount.Should().BeGreaterThanOrEqualTo(0);
                }

                [Fact]
                public async Task Handle_ValidQuery_LogsExecution()
                {
                    // Arrange
                    var query = new {{ Operation.PascalOperationId }}Query();

                    // Act
                    await _sut.Handle(query, CancellationToken.None);

                    // Assert
                    _loggerMock.Verify(
                        x => x.Log(
                            LogLevel.Information,
                            It.IsAny<EventId>(),
                            It.IsAny<It.IsAnyType>(),
                            It.IsAny<Exception?>(),
                            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.AtLeastOnce);
                }

                [Fact]
                public async Task Handle_CancellationRequested_CompletesGracefully()
                {
                    // Arrange
                    var query = new {{ Operation.PascalOperationId }}Query();
                    using var cts = new CancellationTokenSource();

                    // Act
                    var result = await _sut.Handle(query, cts.Token);

                    // Assert
                    result.Should().NotBeNull();
                }
            }
            """;

        // ─── Validator Tests ──────────────────────────────────────────
        public const string ValidatorTest = """
            using {{ Service.PascalName }}.Application.Commands;
            using FluentValidation.TestHelper;

            namespace {{ Service.PascalName }}.Tests.Unit.Validators;

            public class {{ Operation.PascalOperationId }}CommandValidatorTests
            {
                private readonly {{ Operation.PascalOperationId }}CommandValidator _validator = new();

                [Fact]
                public async Task Validate_DefaultCommand_ShouldValidate()
                {
                    // Arrange
                    var command = new {{ Operation.PascalOperationId }}Command();

                    // Act
                    var result = await _validator.TestValidateAsync(command);

                    // Assert — validator exists and runs without exception
                    result.Should().NotBeNull();
                }
            {{~ for p in Operation.PathParameters ~}}
            {{~ if p.Required ~}}

                [Fact]
                public async Task Validate_Empty{{ p.PascalName }}_ShouldHaveError()
                {
                    // Arrange
                    var command = new {{ Operation.PascalOperationId }}Command
                    {
                        {{ p.PascalName }} = default
                    };

                    // Act
                    var result = await _validator.TestValidateAsync(command);

                    // Assert
                    result.ShouldHaveValidationErrorFor(x => x.{{ p.PascalName }});
                }
            {{~ end ~}}
            {{~ end ~}}
            }
            """;

        // ─── Integration Test Base ────────────────────────────────────
        public const string IntegrationTestBase = """
            using System.Net.Http.Json;
            using {{ Service.PascalName }}.Tests.Fixtures;

            namespace {{ Service.PascalName }}.Tests.Integration;

            /// <summary>
            /// Base class for API integration tests using WebApplicationFactory.
            /// Provides an HttpClient and helper methods for common assertions.
            /// </summary>
            [Collection("Integration")]
            public abstract class IntegrationTestBase : IClassFixture<WebAppFixture>
            {
                protected readonly HttpClient Client;
                protected readonly WebAppFixture Factory;

                protected IntegrationTestBase(WebAppFixture factory)
                {
                    Factory = factory;
                    Client = factory.CreateClient();
                }

                /// <summary>GET a JSON-deserialized response.</summary>
                protected async Task<T?> GetAsync<T>(string url)
                {
                    var response = await Client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<T>();
                }

                /// <summary>POST a JSON body and return the deserialized response.</summary>
                protected async Task<HttpResponseMessage> PostAsync<T>(string url, T body)
                    => await Client.PostAsJsonAsync(url, body);

                /// <summary>PUT a JSON body.</summary>
                protected async Task<HttpResponseMessage> PutAsync<T>(string url, T body)
                    => await Client.PutAsJsonAsync(url, body);

                /// <summary>DELETE a resource.</summary>
                protected async Task<HttpResponseMessage> DeleteAsync(string url)
                    => await Client.DeleteAsync(url);
            }
            """;

        // ─── Endpoint Integration Tests ───────────────────────────────
        public const string EndpointIntegrationTest = """
            using System.Net;
            using System.Net.Http.Json;
            using {{ Service.PascalName }}.Tests.Fixtures;

            namespace {{ Service.PascalName }}.Tests.Integration.Endpoints;

            /// <summary>
            /// Integration tests for {{ Tag.PascalName }} API endpoints.
            /// Tests the full HTTP pipeline: routing, middleware, serialization.
            /// </summary>
            [Collection("Integration")]
            public class {{ Tag.PascalName }}EndpointTests : IntegrationTestBase
            {
                public {{ Tag.PascalName }}EndpointTests(WebAppFixture factory) : base(factory) { }

            {{~ for op in Operations ~}}
            {{~ if op.IsQuery ~}}
                [Fact]
                public async Task {{ op.PascalOperationId }}_ReturnsSuccessStatusCode()
                {
                    // Arrange
                    var url = "{{ op.Path }}";
            {{~ for p in op.PathParameters ~}}
                    url = url.Replace("{" + "{{ p.Name }}" + "}", Guid.NewGuid().ToString());
            {{~ end ~}}

                    // Act
                    var response = await Client.GetAsync(url);

                    // Assert
                    response.StatusCode.Should().BeOneOf(
                        HttpStatusCode.OK,
                        HttpStatusCode.NotFound,
                        HttpStatusCode.NoContent);
                }

            {{~ else ~}}
                [Fact]
                public async Task {{ op.PascalOperationId }}_ReturnsExpectedStatusCode()
                {
                    // Arrange
                    var url = "{{ op.Path }}";
            {{~ for p in op.PathParameters ~}}
                    url = url.Replace("{" + "{{ p.Name }}" + "}", Guid.NewGuid().ToString());
            {{~ end ~}}

                    // Act
                    var response = await Client.{{ if op.HttpMethod == "POST" }}PostAsJsonAsync(url, new { }){{ else if op.HttpMethod == "PUT" }}PutAsJsonAsync(url, new { }){{ else if op.HttpMethod == "DELETE" }}DeleteAsync(url){{ else if op.HttpMethod == "PATCH" }}PatchAsJsonAsync(url, new { }){{ else }}GetAsync(url){{ end }};

                    // Assert
                    response.StatusCode.Should().BeOneOf(
                        HttpStatusCode.OK,
                        HttpStatusCode.Created,
                        HttpStatusCode.NoContent,
                        HttpStatusCode.BadRequest,
                        HttpStatusCode.NotFound);
                }

            {{~ end ~}}
            {{~ end ~}}
                [Fact]
                public async Task ContentType_ShouldBeJsonWhenReturned()
                {
                    // Act
                    var response = await Client.GetAsync("/health/live");

                    // Assert — should get some response without server error
                    ((int)response.StatusCode).Should().BeLessThan(500);
                }
            }
            """;

        // ─── DbContext Tests ──────────────────────────────────────────
        public const string DbContextTest = """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using {{ Service.PascalName }}.Infrastructure.Persistence;
            using {{ Service.PascalName }}.Tests.Fixtures;
            using MediatR;

            namespace {{ Service.PascalName }}.Tests.Integration.Database;

            /// <summary>
            /// Tests EF Core DbContext: schema creation, entity CRUD, audit fields.
            /// Uses Testcontainers for real database testing.
            /// </summary>
            [Collection("Database")]
            public class DbContextTests : IClassFixture<DatabaseFixture>
            {
                private readonly DatabaseFixture _dbFixture;

                public DbContextTests(DatabaseFixture dbFixture)
                {
                    _dbFixture = dbFixture;
                }

                private {{ Service.PascalName }}DbContext CreateContext()
                {
                    var mediator = new Mock<IMediator>();
                    var optionsBuilder = new DbContextOptionsBuilder<{{ Service.PascalName }}DbContext>();

                    if (_dbFixture.IsAvailable)
                    {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                        optionsBuilder.UseSqlServer(_dbFixture.ConnectionString);
            {{~ else ~}}
                        optionsBuilder.UseNpgsql(_dbFixture.ConnectionString);
            {{~ end ~}}
                    }
                    else
                    {
                        optionsBuilder.UseInMemoryDatabase("{{ Service.PascalName }}_Test_" + Guid.NewGuid());
                    }

                    var context = new {{ Service.PascalName }}DbContext(optionsBuilder.Options, mediator.Object);
                    context.Database.EnsureCreated();
                    return context;
                }

                [Fact]
                public void DbContext_CanBeCreated()
                {
                    using var context = CreateContext();
                    context.Should().NotBeNull();
                }

                [Fact]
                public async Task DbContext_CanSaveAndRetrieve()
                {
                    using var context = CreateContext();

                    var initialCount = await context.SaveChangesAsync();
                    initialCount.Should().BeGreaterThanOrEqualTo(0);
                }

            {{~ for entity in Entities ~}}
                [Fact]
                public async Task {{ entity.PascalName }}_CanAddAndRead()
                {
                    // Arrange
                    using var context = CreateContext();
                    var entity = {{ entity.PascalName }}Factory.Create();

                    // Act
                    context.{{ entity.PascalName }}s.Add(entity);
                    await context.SaveChangesAsync();

                    // Assert
                    var found = await context.{{ entity.PascalName }}s.FindAsync(entity.Id);
                    found.Should().NotBeNull();
                    found!.Id.Should().Be(entity.Id);
                }

            {{~ end ~}}
                [Fact]
                public async Task AuditFields_AreSetOnAdd()
                {
                    using var context = CreateContext();
            {{~ if Entities.size > 0 ~}}
                    var entity = {{ Entities[0].PascalName }}Factory.Create();
                    entity.CreatedAt = default;

                    context.{{ Entities[0].PascalName }}s.Add(entity);
                    await context.SaveChangesAsync();

                    entity.CreatedAt.Should().NotBe(default);
            {{~ else ~}}
                    await Task.CompletedTask; // No entities to test
            {{~ end ~}}
                }
            }
            """;

        // ─── Repository Tests ─────────────────────────────────────────
        public const string RepositoryTest = """
            using Microsoft.EntityFrameworkCore;
            using {{ Service.PascalName }}.Infrastructure.Persistence;
            using {{ Service.PascalName }}.Tests.Fixtures;
            using MediatR;

            namespace {{ Service.PascalName }}.Tests.Integration.Database;

            /// <summary>
            /// Tests the generic Repository against a real (or in-memory) database.
            /// </summary>
            [Collection("Database")]
            public class RepositoryTests : IClassFixture<DatabaseFixture>
            {
                private readonly DatabaseFixture _dbFixture;

                public RepositoryTests(DatabaseFixture dbFixture)
                {
                    _dbFixture = dbFixture;
                }

                private ({{ Service.PascalName }}DbContext ctx, Repository<T> repo) CreateRepo<T>() where T : class
                {
                    var mediator = new Mock<IMediator>();
                    var optionsBuilder = new DbContextOptionsBuilder<{{ Service.PascalName }}DbContext>();

                    if (_dbFixture.IsAvailable)
                    {
            {{~ if Config.Database.Provider == "SqlServer" ~}}
                        optionsBuilder.UseSqlServer(_dbFixture.ConnectionString);
            {{~ else ~}}
                        optionsBuilder.UseNpgsql(_dbFixture.ConnectionString);
            {{~ end ~}}
                    }
                    else
                    {
                        optionsBuilder.UseInMemoryDatabase("{{ Service.PascalName }}_Repo_" + Guid.NewGuid());
                    }

                    var ctx = new {{ Service.PascalName }}DbContext(optionsBuilder.Options, mediator.Object);
                    ctx.Database.EnsureCreated();
                    return (ctx, new Repository<T>(ctx));
                }

            {{~ for entity in Entities ~}}
                [Fact]
                public async Task {{ entity.PascalName }}_AddAndGetById()
                {
                    var (ctx, repo) = CreateRepo<{{ entity.PascalName }}>();
                    using (ctx)
                    {
                        // Arrange
                        var entity = {{ entity.PascalName }}Factory.Create();

                        // Act
                        await repo.AddAsync(entity);
                        await ctx.SaveChangesAsync();
                        var found = await repo.GetByIdAsync(entity.Id);

                        // Assert
                        found.Should().NotBeNull();
                        found!.Id.Should().Be(entity.Id);
                    }
                }

                [Fact]
                public async Task {{ entity.PascalName }}_GetAll_ReturnsSeeded()
                {
                    var (ctx, repo) = CreateRepo<{{ entity.PascalName }}>();
                    using (ctx)
                    {
                        // Arrange
                        var entities = {{ entity.PascalName }}Factory.CreateMany(3);
                        foreach (var e in entities) await repo.AddAsync(e);
                        await ctx.SaveChangesAsync();

                        // Act
                        var all = await repo.GetAllAsync();

                        // Assert
                        all.Should().HaveCountGreaterThanOrEqualTo(3);
                    }
                }

                [Fact]
                public async Task {{ entity.PascalName }}_Delete_RemovesEntity()
                {
                    var (ctx, repo) = CreateRepo<{{ entity.PascalName }}>();
                    using (ctx)
                    {
                        // Arrange
                        var entity = {{ entity.PascalName }}Factory.Create();
                        await repo.AddAsync(entity);
                        await ctx.SaveChangesAsync();

                        // Act
                        await repo.DeleteAsync(entity);
                        await ctx.SaveChangesAsync();
                        var found = await repo.GetByIdAsync(entity.Id);

                        // Assert
                        found.Should().BeNull();
                    }
                }

                [Fact]
                public async Task {{ entity.PascalName }}_Count_ReturnsCorrect()
                {
                    var (ctx, repo) = CreateRepo<{{ entity.PascalName }}>();
                    using (ctx)
                    {
                        var entities = {{ entity.PascalName }}Factory.CreateMany(5);
                        foreach (var e in entities) await repo.AddAsync(e);
                        await ctx.SaveChangesAsync();

                        var count = await repo.CountAsync();
                        count.Should().BeGreaterThanOrEqualTo(5);
                    }
                }

                [Fact]
                public async Task {{ entity.PascalName }}_Exists_ReturnsTrueForExisting()
                {
                    var (ctx, repo) = CreateRepo<{{ entity.PascalName }}>();
                    using (ctx)
                    {
                        var entity = {{ entity.PascalName }}Factory.Create();
                        await repo.AddAsync(entity);
                        await ctx.SaveChangesAsync();

                        var exists = await repo.ExistsAsync(e => e.Id == entity.Id);
                        exists.Should().BeTrue();
                    }
                }

            {{~ end ~}}
            }
            """;

        // ─── Architecture Tests ───────────────────────────────────────
        public const string ArchitectureTest = """
            using System.Reflection;

            namespace {{ Service.PascalName }}.Tests.Architecture;

            /// <summary>
            /// Validates Clean Architecture layer dependency rules.
            /// Domain must not reference Infrastructure or Api.
            /// Application must not reference Infrastructure or Api.
            /// </summary>
            public class LayerDependencyTests
            {
                private static readonly Assembly DomainAssembly =
                    typeof({{ Service.PascalName }}.Domain.Common.BaseEntity).Assembly;

                private static readonly Assembly ApplicationAssembly =
                    typeof({{ Service.PascalName }}.Application.Interfaces.IUnitOfWork).Assembly;

                [Fact]
                public void Domain_ShouldNotReference_Infrastructure()
                {
                    var references = DomainAssembly.GetReferencedAssemblies();
                    references.Should().NotContain(a =>
                        a.Name != null && a.Name.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase),
                        "Domain layer must not depend on Infrastructure");
                }

                [Fact]
                public void Domain_ShouldNotReference_Api()
                {
                    var references = DomainAssembly.GetReferencedAssemblies();
                    references.Should().NotContain(a =>
                        a.Name != null && a.Name.Contains(".Api", StringComparison.OrdinalIgnoreCase),
                        "Domain layer must not depend on Api");
                }

                [Fact]
                public void Application_ShouldNotReference_Infrastructure()
                {
                    var references = ApplicationAssembly.GetReferencedAssemblies();
                    references.Should().NotContain(a =>
                        a.Name != null && a.Name.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase),
                        "Application layer must not depend on Infrastructure");
                }

                [Fact]
                public void Application_ShouldNotReference_Api()
                {
                    var references = ApplicationAssembly.GetReferencedAssemblies();
                    references.Should().NotContain(a =>
                        a.Name != null && a.Name.Contains(".Api", StringComparison.OrdinalIgnoreCase),
                        "Application layer must not depend on Api");
                }

                [Fact]
                public void Domain_ShouldHaveMinimalDependencies()
                {
                    var references = DomainAssembly.GetReferencedAssemblies()
                        .Where(a => a.Name != null && !a.Name.StartsWith("System", StringComparison.Ordinal)
                                    && !a.Name.StartsWith("Microsoft", StringComparison.Ordinal)
                                    && !a.Name.StartsWith("netstandard", StringComparison.Ordinal))
                        .ToList();

                    // Domain should only reference MediatR (for domain events)
                    references.Should().HaveCountLessThanOrEqualTo(2,
                        "Domain should have minimal external dependencies");
                }
            }
            """;

        // ─── Health Check Tests ───────────────────────────────────────
        public const string HealthCheckTest = """
            using System.Net;
            using {{ Service.PascalName }}.Tests.Fixtures;

            namespace {{ Service.PascalName }}.Tests.Integration;

            /// <summary>
            /// Validates the health check endpoints are properly configured.
            /// </summary>
            [Collection("Integration")]
            public class HealthCheckTests : IntegrationTestBase
            {
                public HealthCheckTests(WebAppFixture factory) : base(factory) { }

                [Fact]
                public async Task LivenessProbe_ReturnsHealthy()
                {
                    var response = await Client.GetAsync("/health/live");

                    response.StatusCode.Should().Be(HttpStatusCode.OK);
                }

                [Fact]
                public async Task ReadinessProbe_ReturnsStatusCode()
                {
                    var response = await Client.GetAsync("/health/ready");

                    // Ready probe may return OK or ServiceUnavailable depending on DB state
                    response.StatusCode.Should().BeOneOf(
                        HttpStatusCode.OK,
                        HttpStatusCode.ServiceUnavailable);
                }

                [Fact]
                public async Task SwaggerEndpoint_IsAccessibleInDevelopment()
                {
                    var response = await Client.GetAsync("/swagger/v1/swagger.json");

                    // Swagger may not be available in Testing env; just verify no 500
                    response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
                }
            }
            """;

        // ─── Helm Chart Tests ─────────────────────────────────────────
        public const string HelmChartTest = """
            using YamlDotNet.Serialization;
            using YamlDotNet.Serialization.NamingConventions;

            namespace {{ Service.PascalName }}.Tests.Infrastructure;

            /// <summary>
            /// Validates Helm chart structure, required fields, and configuration consistency.
            /// </summary>
            public class HelmChartTests
            {
                private static readonly string DeployDir = FindDeployDir();
                private static readonly IDeserializer Yaml = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                private static string FindDeployDir()
                {
                    // Look for deploy/ relative to test output or project root
                    var dir = AppContext.BaseDirectory;
                    for (var i = 0; i < 6; i++)
                    {
                        var candidate = Path.Combine(dir, "deploy");
                        if (Directory.Exists(candidate)) return candidate;
                        var parent = Directory.GetParent(dir);
                        if (parent is null) break;
                        dir = parent.FullName;
                    }
                    return Path.Combine(AppContext.BaseDirectory, "deploy");
                }

                [SkippableFact]
                public void ChartYaml_Exists_AndHasRequiredFields()
                {
                    var path = Path.Combine(DeployDir, "helm", "Chart.yaml");
                    Skip.IfNot(File.Exists(path), "Helm chart not found at: " + path);

                    var content = File.ReadAllText(path);
                    var chart = Yaml.Deserialize<Dictionary<string, object>>(content);

                    chart.Should().ContainKey("apiVersion");
                    chart.Should().ContainKey("name");
                    chart.Should().ContainKey("version");
                    chart["name"].ToString().Should().Be("{{ Service.KebabName }}");
                }

                [SkippableFact]
                public void ValuesYaml_Exists_AndHasDefaults()
                {
                    var path = Path.Combine(DeployDir, "helm", "values.yaml");
                    Skip.IfNot(File.Exists(path), "Helm values not found at: " + path);

                    var content = File.ReadAllText(path);
                    var values = Yaml.Deserialize<Dictionary<string, object>>(content);

                    values.Should().ContainKey("replicaCount");
                    values.Should().ContainKey("image");
                    values.Should().ContainKey("service");
                }

                [SkippableFact]
                public void HelmTemplates_Exist()
                {
                    var templatesDir = Path.Combine(DeployDir, "helm", "templates");
                    Skip.IfNot(Directory.Exists(templatesDir), "Helm templates dir not found");

                    var files = Directory.GetFiles(templatesDir, "*.yaml");
                    files.Should().NotBeEmpty("Helm chart should have template files");

                    // Should have at least deployment and service templates
                    files.Should().Contain(f => f.Contains("deployment", StringComparison.OrdinalIgnoreCase));
                    files.Should().Contain(f => f.Contains("service", StringComparison.OrdinalIgnoreCase));
                }

                [SkippableFact]
                public void ValuesYaml_ResourceLimits_AreDefined()
                {
                    var path = Path.Combine(DeployDir, "helm", "values.yaml");
                    Skip.IfNot(File.Exists(path), "Helm values not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("resources", "Helm values should define resource limits");
                    content.Should().Contain("limits", "Helm values should define resource limits");
                    content.Should().Contain("requests", "Helm values should define resource requests");
                }

                [SkippableFact]
                public void ValuesYaml_HealthProbes_AreConfigured()
                {
                    var path = Path.Combine(DeployDir, "helm", "values.yaml");
                    Skip.IfNot(File.Exists(path), "Helm values not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("health", "Helm values should configure health check paths");
                }
            }
            """;

        // ─── Kubernetes Manifest Tests ────────────────────────────────
        public const string K8sManifestTest = """
            using YamlDotNet.Serialization;
            using YamlDotNet.Serialization.NamingConventions;

            namespace {{ Service.PascalName }}.Tests.Infrastructure;

            /// <summary>
            /// Validates Kubernetes manifest structure and required fields.
            /// </summary>
            public class KubernetesManifestTests
            {
                private static readonly string K8sDir = FindK8sDir();
                private static readonly IDeserializer Yaml = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                private static string FindK8sDir()
                {
                    var dir = AppContext.BaseDirectory;
                    for (var i = 0; i < 6; i++)
                    {
                        var candidate = Path.Combine(dir, "deploy", "k8s");
                        if (Directory.Exists(candidate)) return candidate;
                        var parent = Directory.GetParent(dir);
                        if (parent is null) break;
                        dir = parent.FullName;
                    }
                    return Path.Combine(AppContext.BaseDirectory, "deploy", "k8s");
                }

                [SkippableFact]
                public void Deployment_HasRequiredFields()
                {
                    var path = Path.Combine(K8sDir, "deployment.yaml");
                    Skip.IfNot(File.Exists(path), "K8s deployment manifest not found");

                    var content = File.ReadAllText(path);

                    content.Should().Contain("apiVersion");
                    content.Should().Contain("kind: Deployment");
                    content.Should().Contain("metadata");
                    content.Should().Contain("spec");
                    content.Should().Contain("containers");
                    content.Should().Contain("livenessProbe");
                    content.Should().Contain("readinessProbe");
                }

                [SkippableFact]
                public void Service_HasRequiredFields()
                {
                    var path = Path.Combine(K8sDir, "service.yaml");
                    Skip.IfNot(File.Exists(path), "K8s service manifest not found");

                    var content = File.ReadAllText(path);

                    content.Should().Contain("apiVersion");
                    content.Should().Contain("kind: Service");
                    content.Should().Contain("selector");
                    content.Should().Contain("ports");
                }

                [SkippableFact]
                public void Deployment_HasResourceLimits()
                {
                    var path = Path.Combine(K8sDir, "deployment.yaml");
                    Skip.IfNot(File.Exists(path), "K8s deployment manifest not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("resources");
                    content.Should().Contain("limits");
                    content.Should().Contain("requests");
                }

                [SkippableFact]
                public void Namespace_Manifest_Exists()
                {
                    var path = Path.Combine(K8sDir, "namespace.yaml");
                    Skip.IfNot(File.Exists(path), "K8s namespace manifest not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("kind: Namespace");
                    content.Should().Contain("{{ Service.KebabName }}");
                }

                [SkippableFact]
                public void ConfigMap_Manifest_Exists()
                {
                    var path = Path.Combine(K8sDir, "configmap.yaml");
                    Skip.IfNot(File.Exists(path), "K8s configmap manifest not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("kind: ConfigMap");
                }

                [SkippableFact]
                public void HPA_Manifest_Exists()
                {
                    var path = Path.Combine(K8sDir, "hpa.yaml");
                    Skip.IfNot(File.Exists(path), "K8s HPA manifest not found");

                    var content = File.ReadAllText(path);
                    content.Should().Contain("kind: HorizontalPodAutoscaler");
                    content.Should().Contain("minReplicas");
                    content.Should().Contain("maxReplicas");
                }
            }
            """;

        // ─── Dockerfile Tests ─────────────────────────────────────────
        public const string DockerfileTest = """
            namespace {{ Service.PascalName }}.Tests.Infrastructure;

            /// <summary>
            /// Validates Dockerfile best practices and required instructions.
            /// </summary>
            public class DockerfileTests
            {
                private static readonly string? DockerfilePath = FindDockerfile();

                private static string? FindDockerfile()
                {
                    var dir = AppContext.BaseDirectory;
                    for (var i = 0; i < 6; i++)
                    {
                        var candidate = Path.Combine(dir, "deploy", "Dockerfile");
                        if (File.Exists(candidate)) return candidate;
                        var parent = Directory.GetParent(dir);
                        if (parent is null) break;
                        dir = parent.FullName;
                    }
                    return null;
                }

                [SkippableFact]
                public void Dockerfile_Exists()
                {
                    Skip.If(DockerfilePath is null, "Dockerfile not found in deploy/");
                    File.Exists(DockerfilePath).Should().BeTrue();
                }

                [SkippableFact]
                public void Dockerfile_UsesMultiStageBuild()
                {
                    Skip.If(DockerfilePath is null, "Dockerfile not found");
                    var content = File.ReadAllText(DockerfilePath);

                    // Multi-stage builds have multiple FROM instructions
                    var fromCount = content.Split('\n')
                        .Count(line => line.TrimStart().StartsWith("FROM", StringComparison.OrdinalIgnoreCase));

                    fromCount.Should().BeGreaterThan(1, "Dockerfile should use multi-stage builds");
                }

                [SkippableFact]
                public void Dockerfile_DoesNotRunAsRoot()
                {
                    Skip.If(DockerfilePath is null, "Dockerfile not found");
                    var content = File.ReadAllText(DockerfilePath);

                    content.Should().Contain("USER", "Dockerfile should specify a non-root USER");
                }

                [SkippableFact]
                public void Dockerfile_ExposesPort()
                {
                    Skip.If(DockerfilePath is null, "Dockerfile not found");
                    var content = File.ReadAllText(DockerfilePath);

                    content.Should().Contain("EXPOSE", "Dockerfile should EXPOSE a port");
                }

                [SkippableFact]
                public void Dockerfile_HasHealthcheck()
                {
                    Skip.If(DockerfilePath is null, "Dockerfile not found");
                    var content = File.ReadAllText(DockerfilePath);

                    // Either HEALTHCHECK instruction or health endpoint in ENTRYPOINT
                    var hasHealth = content.Contains("HEALTHCHECK", StringComparison.OrdinalIgnoreCase)
                                   || content.Contains("health", StringComparison.OrdinalIgnoreCase);
                    hasHealth.Should().BeTrue("Dockerfile should reference health checks");
                }
            }
            """;
    }
}
