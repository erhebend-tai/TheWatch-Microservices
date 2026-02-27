using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Hangfire Jobs project: recurring jobs, batch jobs, configuration.
/// </summary>
public sealed class JobsProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public JobsProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Jobs";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // Hangfire configuration
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "HangfireConfiguration.cs",
            _engine.Render(Templates.HangfireConfig, new { Service = service, Config = _config }),
            ct);

        // Recurring job base
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Recurring", "BaseRecurringJob.cs"),
            _engine.Render(Templates.BaseRecurringJob, new { Service = service }),
            ct);

        // Health check job
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Recurring", "HealthCheckJob.cs"),
            _engine.Render(Templates.HealthCheckJob, new { Service = service }),
            ct);

        // Data sync job
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Recurring", "DataSyncJob.cs"),
            _engine.Render(Templates.DataSyncJob, new { Service = service }),
            ct);

        // Cleanup job
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Recurring", "CleanupJob.cs"),
            _engine.Render(Templates.CleanupJob, new { Service = service }),
            ct);

        // Batch job base
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Batch", "BaseBatchJob.cs"),
            _engine.Render(Templates.BaseBatchJob, new { Service = service }),
            ct);

        // Job registration
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "JobRegistration.cs",
            _engine.Render(Templates.JobRegistration, new { Service = service }),
            ct);

        // DI extension
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "DependencyInjection.cs",
            _engine.Render(Templates.DependencyInjection, new { Service = service, Config = _config }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <RootNamespace>{{ Service.PascalName }}.Jobs</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Hangfire.Core" Version="1.*" />
                <PackageReference Include="Hangfire.Pro" Version="3.*" />
                <PackageReference Include="Hangfire.Pro.Redis" Version="3.*" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Application\{{ Service.PascalName }}.Application.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Infrastructure\{{ Service.PascalName }}.Infrastructure.csproj" />
              </ItemGroup>
            </Project>
            """;

        public const string HangfireConfig = """
            using Hangfire;
            using Hangfire.Pro.Redis;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{ Service.PascalName }}.Jobs;

            public static class HangfireConfiguration
            {
                public static IServiceCollection AddHangfireServices(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
                    services.AddHangfire(config => config
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseRedisStorage(
                            configuration.GetConnectionString("Redis"),
                            new RedisStorageOptions
                            {
                                Prefix = "{{ Service.KebabName }}:hangfire:",
                                Db = 1
                            }));

                    services.AddHangfireServer(options =>
                    {
                        options.ServerName = "{{ Service.PascalName }}";
                        options.WorkerCount = Environment.ProcessorCount * 2;
                        options.Queues = ["critical", "default", "low"];
                    });

                    return services;
                }
            }
            """;

        public const string BaseRecurringJob = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Jobs.Recurring;

            public abstract class BaseRecurringJob
            {
                protected readonly ILogger Logger;

                protected BaseRecurringJob(ILogger logger) => Logger = logger;

                public async Task ExecuteAsync()
                {
                    var jobName = GetType().Name;
                    Logger.LogInformation("Starting recurring job: {JobName}", jobName);

                    try
                    {
                        await RunAsync();
                        Logger.LogInformation("Completed recurring job: {JobName}", jobName);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed recurring job: {JobName}", jobName);
                        throw;
                    }
                }

                protected abstract Task RunAsync();
            }
            """;

        public const string HealthCheckJob = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Jobs.Recurring;

            /// <summary>
            /// Periodic health check job that verifies service dependencies.
            /// Runs every 5 minutes by default.
            /// </summary>
            public sealed class HealthCheckJob : BaseRecurringJob
            {
                public HealthCheckJob(ILogger<HealthCheckJob> logger) : base(logger) { }

                protected override async Task RunAsync()
                {
                    // TODO: Check database connectivity
                    // TODO: Check Redis connectivity
                    // TODO: Check external service health
                    await Task.CompletedTask;
                    Logger.LogInformation("All health checks passed for {{ Service.PascalName }}");
                }
            }
            """;

        public const string DataSyncJob = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Jobs.Recurring;

            /// <summary>
            /// Periodic data synchronization job.
            /// Runs every 15 minutes by default.
            /// </summary>
            public sealed class DataSyncJob : BaseRecurringJob
            {
                public DataSyncJob(ILogger<DataSyncJob> logger) : base(logger) { }

                protected override async Task RunAsync()
                {
                    // TODO: Implement data synchronization logic
                    await Task.CompletedTask;
                    Logger.LogInformation("Data sync completed for {{ Service.PascalName }}");
                }
            }
            """;

        public const string CleanupJob = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Jobs.Recurring;

            /// <summary>
            /// Periodic cleanup of expired/stale data.
            /// Runs daily at 2:00 AM by default.
            /// </summary>
            public sealed class CleanupJob : BaseRecurringJob
            {
                public CleanupJob(ILogger<CleanupJob> logger) : base(logger) { }

                protected override async Task RunAsync()
                {
                    // TODO: Clean up expired sessions
                    // TODO: Archive old records
                    // TODO: Purge temporary files
                    await Task.CompletedTask;
                    Logger.LogInformation("Cleanup completed for {{ Service.PascalName }}");
                }
            }
            """;

        public const string BaseBatchJob = """
            using Hangfire;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Jobs.Batch;

            public abstract class BaseBatchJob
            {
                protected readonly ILogger Logger;

                protected BaseBatchJob(ILogger logger) => Logger = logger;

                [AutomaticRetry(Attempts = 3)]
                public async Task ExecuteAsync(string batchId)
                {
                    var jobName = GetType().Name;
                    Logger.LogInformation("Starting batch job: {JobName} (Batch: {BatchId})", jobName, batchId);

                    try
                    {
                        await RunAsync(batchId);
                        Logger.LogInformation("Completed batch job: {JobName} (Batch: {BatchId})", jobName, batchId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed batch job: {JobName} (Batch: {BatchId})", jobName, batchId);
                        throw;
                    }
                }

                protected abstract Task RunAsync(string batchId);
            }
            """;

        public const string JobRegistration = """
            using {{ Service.PascalName }}.Jobs.Recurring;
            using Hangfire;

            namespace {{ Service.PascalName }}.Jobs;

            public static class JobRegistration
            {
                public static void RegisterRecurringJobs()
                {
                    RecurringJob.AddOrUpdate<HealthCheckJob>(
                        "{{ Service.KebabName }}-health-check",
                        job => job.ExecuteAsync(),
                        "*/5 * * * *"); // Every 5 minutes

                    RecurringJob.AddOrUpdate<DataSyncJob>(
                        "{{ Service.KebabName }}-data-sync",
                        job => job.ExecuteAsync(),
                        "*/15 * * * *"); // Every 15 minutes

                    RecurringJob.AddOrUpdate<CleanupJob>(
                        "{{ Service.KebabName }}-cleanup",
                        job => job.ExecuteAsync(),
                        "0 2 * * *"); // Daily at 2 AM
                }
            }
            """;

        public const string DependencyInjection = """
            using {{ Service.PascalName }}.Jobs.Recurring;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{ Service.PascalName }}.Jobs;

            public static class DependencyInjection
            {
                public static IServiceCollection AddJobServices(this IServiceCollection services)
                {
                    services.AddTransient<HealthCheckJob>();
                    services.AddTransient<DataSyncJob>();
                    services.AddTransient<CleanupJob>();

                    return services;
                }
            }
            """;
    }
}
