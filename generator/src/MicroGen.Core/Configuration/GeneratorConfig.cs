namespace MicroGen.Core.Configuration;

/// <summary>
/// Configuration for the microservice generator.
/// </summary>
public sealed record GeneratorConfig
{
    public string TargetFramework { get; init; } = "net10.0";
    public string OutputDirectory { get; init; } = "./services";
    public string OutputStructure { get; init; } = "domain-grouped"; // domain-grouped | flat

    public FeatureConfig Features { get; init; } = new();
    public LoggingConfig Logging { get; init; } = new();
    public TelemetryConfig Telemetry { get; init; } = new();
    public CachingConfig Caching { get; init; } = new();
    public DatabaseConfig Database { get; init; } = new();
    public CICDConfig CICD { get; init; } = new();

    public ApacheConfig Apache { get; init; } = new();
    public DaprConfig Dapr { get; init; } = new();

    public string[] SourceTypes { get; init; } = ["openapi", "sql", "csv", "graphql", "html"];
    public string[] UrlSources { get; init; } = [];

    public string[] IncludePatterns { get; init; } = ["*.yaml", "*.yml", "*.json", "*.sql", "*.csv", "*.gql", "*.graphql"];
    public string[] ExcludePatterns { get; init; } = ["_shared/**", "_testing/**", "node_modules/**"];
    public bool Recursive { get; init; } = true;
    public bool SkipTests { get; init; }
    public bool DryRun { get; init; }
}

public sealed record FeatureConfig
{
    public bool Hangfire { get; init; } = true;
    public bool BlazorDashboard { get; init; } = true;
    public bool MauiBlazorApp { get; init; } = true;
    public bool RadzenUI { get; init; } = true;
    public bool VoiceInterface { get; init; } = true;
    public bool ServiceExplorer { get; init; } = true;
    public bool Redis { get; init; } = true;
    public bool OpenTelemetry { get; init; } = true;
    public bool SignalR { get; init; } = true;
    public bool Polly { get; init; } = true;
    public bool Serilog { get; init; } = true;
    public bool VoicePipeline { get; init; } = true;
    public bool GitHubActions { get; init; } = true;
    public bool TeamCity { get; init; } = true;
    public bool CodeQL { get; init; } = true;
    public bool SymbolIndexing { get; init; } = true;
    public bool AspireAppHost { get; init; } = true;

    // Apache Infrastructure Stack
    public bool Kafka { get; init; } = true;
    public bool OpenWhisk { get; init; } = true;
    public bool Dubbo { get; init; } = true;

    // Apache Analytics Stack
    public bool ECharts { get; init; } = true;
    public bool Superset { get; init; } = true;

    // Dapr
    public bool Dapr { get; init; } = true;
}

public sealed record LoggingConfig
{
    public string Framework { get; init; } = "serilog";
    public string[] Sinks { get; init; } = ["Console", "File"];
    public string MinimumLevel { get; init; } = "Information";
    public string[] DefaultSinks { get; init; } = ["console", "file"];
    public string[] AvailableSinks { get; init; } = ["console", "file", "sqlserver", "postgresql", "mongodb"];
}

public sealed record TelemetryConfig
{
    public string Framework { get; init; } = "opentelemetry";
    public string[] Exporters { get; init; } = ["Console", "Prometheus", "OTLP"];
    public string[] DefaultExporters { get; init; } = ["console", "prometheus"];
    public string[] AvailableExporters { get; init; } = ["console", "prometheus", "otlp"];
}

public sealed record CachingConfig
{
    public string DefaultBackend { get; init; } = "redis";
    public string[] AvailableBackends { get; init; } = ["memory", "redis", "sqlserver", "postgresql"];
    public int DefaultTtlSeconds { get; init; } = 300;
    public string Provider { get; init; } = "Redis";
}

public sealed record DatabaseConfig
{
    public string Provider { get; init; } = "postgresql";
    public string[] AvailableProviders { get; init; } = ["sqlserver", "postgresql", "mongodb"];
    public bool MigrationsEnabled { get; init; } = true;
    public bool SeedDataEnabled { get; init; } = true;
}

public sealed record ApacheConfig
{
    // Kafka
    public string KafkaVersion { get; init; } = "3.9";
    public int KafkaReplicas { get; init; } = 3;
    public int KafkaReplicationFactor { get; init; } = 3;
    public string SchemaRegistryVersion { get; init; } = "7.7";
    public bool KafkaUI { get; init; } = true;
    public string KafkaDeploymentMode { get; init; } = "strimzi"; // strimzi | bitnami

    // OpenWhisk
    public string OpenWhiskVersion { get; init; } = "1.0";
    public int OpenWhiskInvokerReplicas { get; init; } = 2;

    // Dubbo
    public string DubboVersion { get; init; } = "3.3";
    public string DubboRegistryType { get; init; } = "nacos"; // nacos | zookeeper
    public string NacosVersion { get; init; } = "2.4";

    // Superset
    public string SupersetVersion { get; init; } = "4.1";
    public int SupersetWorkers { get; init; } = 2;

    // ECharts
    public string EChartsVersion { get; init; } = "5.5";
}

public sealed record DaprConfig
{
    public string DaprVersion { get; init; } = "1.14";
    public string StateStoreName { get; init; } = "statestore";
    public string PubSubName { get; init; } = "messages";
    public string SecretStoreName { get; init; } = "secretstore";
    public string StateStoreType { get; init; } = "state.redis"; // state.redis | state.postgresql | state.cosmosdb
    public string PubSubType { get; init; } = "pubsub.redis"; // pubsub.redis | pubsub.kafka
    public string SecretStoreType { get; init; } = "secretstores.local.file"; // secretstores.local.file | secretstores.kubernetes | secretstores.azure.keyvault
    public bool VoiceEndpoints { get; init; } = true;
    public bool AzureContainerApps { get; init; } = true;
}

public sealed record CICDConfig
{
    public string ContainerRegistry { get; init; } = "ghcr.io";
    public string RegistryOwner { get; init; } = "thewatch";
    public string DefaultBranch { get; init; } = "main";
    public string[] ProtectedBranches { get; init; } = ["main", "develop"];
    public string[] CodeQLLanguages { get; init; } = ["csharp"];
    public string CodeQLSchedule { get; init; } = "0 6 * * 1"; // Weekly Monday 6AM
    public bool EnableDependabot { get; init; } = true;
    public bool EnableSecurityScanning { get; init; } = true;
}
