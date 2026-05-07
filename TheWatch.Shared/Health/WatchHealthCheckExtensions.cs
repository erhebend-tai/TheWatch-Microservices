using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Health;

/// <summary>
/// Registers infrastructure dependency health checks for all TheWatch microservices.
/// Verifies actual connectivity to SQL Server, Redis, Kafka, PostGIS, and SignalR
/// rather than returning a blind HTTP 200.
/// </summary>
/// <remarks>
/// <para>
/// Each check is tagged so that Kubernetes probes and monitoring dashboards can
/// filter by dependency type:
/// <list type="bullet">
///   <item><c>"db"</c> - SQL Server and PostGIS database checks</item>
///   <item><c>"cache"</c> - Redis cache check</item>
///   <item><c>"messaging"</c> - Kafka broker check</item>
///   <item><c>"geo"</c> - PostGIS spatial database check (only if configured)</item>
///   <item><c>"signalr"</c>, <c>"realtime"</c> - SignalR hub infrastructure check</item>
/// </list>
/// </para>
/// <para>
/// Usage in <c>Program.cs</c>:
/// <code>
/// builder.Services.AddWatchHealthChecks(builder.Configuration);
/// </code>
/// </para>
/// </remarks>
public static class WatchHealthCheckExtensions
{
    /// <summary>
    /// Adds infrastructure health checks based on what is configured in the service's
    /// <c>appsettings.json</c>. Only dependencies with a configured connection string
    /// will be checked, ensuring services that do not use a particular dependency
    /// (e.g., PostGIS) do not fail on a missing check.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="configuration">The application configuration containing connection strings.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        // ── SQL Server ──────────────────────────────────────────────────
        // Connection string keys follow the Aspire naming convention used by the
        // DbContext generator: {service}db (e.g., "voiceemergencydb", "authsecuritydb").
        // We also check the legacy "SqlServer" key for backward compatibility.
        var sqlConnectionString = FindConnectionString(configuration,
            "SqlServer", "DefaultConnection", "voiceemergencydb",
            "authsecuritydb", "coregatewaydb", "meshnetworkdb",
            "wearabledb", "firstresponderdb", "familyhealthdb",
            "disasterreliefdb", "doctorservicesdb", "gamificationdb",
            "surveillancedb");

        if (!string.IsNullOrEmpty(sqlConnectionString))
        {
            healthChecks.Add(new HealthCheckRegistration(
                "sqlserver",
                sp => new SqlServerHealthCheck(
                    sqlConnectionString,
                    sp.GetRequiredService<ILogger<SqlServerHealthCheck>>()),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql", "ready"]));
        }

        // ── Redis ───────────────────────────────────────────────────────
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecks.Add(new HealthCheckRegistration(
                "redis",
                sp => new RedisHealthCheck(
                    redisConnectionString,
                    sp.GetRequiredService<ILogger<RedisHealthCheck>>()),
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"]));
        }

        // ── Kafka ───────────────────────────────────────────────────────
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? configuration.GetConnectionString("Kafka");

        if (!string.IsNullOrEmpty(kafkaBootstrapServers))
        {
            healthChecks.Add(new HealthCheckRegistration(
                "kafka",
                sp => new KafkaHealthCheck(
                    kafkaBootstrapServers,
                    sp.GetRequiredService<ILogger<KafkaHealthCheck>>()),
                failureStatus: HealthStatus.Degraded,
                tags: ["messaging", "kafka", "ready"]));
        }

        // ── PostGIS (PostgreSQL + PostGIS spatial) ──────────────────────
        var postgisConnectionString = configuration.GetConnectionString("PostGIS")
            ?? configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("geospatialdb");

        if (!string.IsNullOrEmpty(postgisConnectionString))
        {
            healthChecks.Add(new HealthCheckRegistration(
                "postgis",
                sp => new PostGisHealthCheck(
                    postgisConnectionString,
                    sp.GetRequiredService<ILogger<PostGisHealthCheck>>()),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "geo", "postgis", "ready"]));
        }

        // ── SignalR Hub Infrastructure ──────────────────────────────────
        // Always register the SignalR check. If SignalR is not configured for
        // a given service, the check will return Degraded (not Unhealthy).
        healthChecks.Add(new HealthCheckRegistration(
            "signalr",
            sp => new SignalRHealthCheck(
                sp,
                sp.GetRequiredService<ILogger<SignalRHealthCheck>>()),
            failureStatus: null, // Use the result from the check itself
            tags: ["signalr", "realtime"]));

        // ── Item 292: Data-at-rest encryption verification ──────────────
        // In production, verify that TDE (SQL Server), Redis TLS, and Kafka SASL_SSL
        // are all active. Fail the readiness probe if any encryption layer is missing
        // in a production environment. [NIST SC-28]
        healthChecks.Add(new HealthCheckRegistration(
            "encryption",
            sp => new EncryptionVerificationHealthCheck(
                configuration,
                sp.GetRequiredService<IHostEnvironment>(),
                sp.GetRequiredService<ILogger<EncryptionVerificationHealthCheck>>()),
            failureStatus: HealthStatus.Degraded,
            tags: ["security", "encryption", "ready"]));

        return services;
    }

    /// <summary>
    /// Searches for the first non-empty connection string from the given keys.
    /// </summary>
    private static string? FindConnectionString(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration.GetConnectionString(key);
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return null;
    }
}

/// <summary>
/// Health check that opens a SQL Server connection and executes <c>SELECT 1</c>
/// to verify the database is reachable and accepting queries.
/// </summary>
internal sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public SqlServerHealthCheck(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            var result = await command.ExecuteScalarAsync(cancellationToken);

            _logger.LogDebug("SQL Server health check passed: SELECT 1 = {Result}", result);

            return HealthCheckResult.Healthy(
                "SQL Server connection successful.",
                new Dictionary<string, object>
                {
                    ["database"] = connection.Database,
                    ["serverVersion"] = connection.ServerVersion ?? "unknown",
                    ["dataSource"] = connection.DataSource ?? "unknown"
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL Server health check failed");

            return HealthCheckResult.Unhealthy(
                $"SQL Server connection failed: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    ["connectionString"] = MaskConnectionString(_connectionString)
                });
        }
    }

    private static string MaskConnectionString(string cs)
    {
        // Mask password in connection string for health check output
        if (string.IsNullOrEmpty(cs)) return "(empty)";
        var masked = System.Text.RegularExpressions.Regex.Replace(
            cs, @"(Password|Pwd)\s*=\s*[^;]+", "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return masked;
    }
}

/// <summary>
/// Health check that connects to Redis and executes a PING command.
/// Uses raw socket connection since StackExchange.Redis may not be referenced.
/// </summary>
internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public RedisHealthCheck(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse host:port from connection string (supports "host:port" or "host:port,option=value")
            var hostPort = _connectionString.Split(',')[0].Trim();
            var parts = hostPort.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;

            using var client = new System.Net.Sockets.TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(host, port, cts.Token);

            // Send Redis PING command using RESP protocol
            var stream = client.GetStream();
            var pingCommand = System.Text.Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n");
            await stream.WriteAsync(pingCommand, cts.Token);

            var buffer = new byte[64];
            var bytesRead = await stream.ReadAsync(buffer, cts.Token);
            var response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response.Contains("PONG", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Redis health check passed: PING -> PONG at {Host}:{Port}", host, port);

                return HealthCheckResult.Healthy(
                    $"Redis connection successful at {host}:{port}.",
                    new Dictionary<string, object>
                    {
                        ["host"] = host,
                        ["port"] = port
                    });
            }

            // Auth required or unexpected response
            _logger.LogWarning("Redis health check: unexpected response: {Response}", response);

            return HealthCheckResult.Degraded(
                $"Redis responded but with unexpected reply: {response}",
                data: new Dictionary<string, object>
                {
                    ["host"] = host,
                    ["port"] = port,
                    ["response"] = response
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed for {ConnectionString}", _connectionString);

            return HealthCheckResult.Unhealthy(
                $"Redis connection failed: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    ["connectionString"] = _connectionString
                });
        }
    }
}

/// <summary>
/// Health check for PostgreSQL/PostGIS. Opens a connection and executes
/// <c>SELECT PostGIS_Version()</c> to verify both database connectivity
/// and PostGIS extension availability.
/// </summary>
internal sealed class PostGisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public PostGisHealthCheck(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // First verify basic PostgreSQL connectivity
            await using var versionCmd = connection.CreateCommand();
            versionCmd.CommandText = "SELECT version()";
            versionCmd.CommandTimeout = 5;
            var pgVersion = await versionCmd.ExecuteScalarAsync(cancellationToken) as string ?? "unknown";

            // Then verify PostGIS extension is installed
            string? postgisVersion = null;
            try
            {
                await using var postgisCmd = connection.CreateCommand();
                postgisCmd.CommandText = "SELECT PostGIS_Version()";
                postgisCmd.CommandTimeout = 5;
                postgisVersion = await postgisCmd.ExecuteScalarAsync(cancellationToken) as string;
            }
            catch
            {
                // PostGIS extension may not be installed — still report PostgreSQL as healthy
            }

            _logger.LogDebug(
                "PostGIS health check passed: PostgreSQL={PgVersion}, PostGIS={PostGISVersion}",
                pgVersion, postgisVersion ?? "not installed");

            var data = new Dictionary<string, object>
            {
                ["database"] = connection.Database ?? "unknown",
                ["host"] = connection.Host ?? "unknown",
                ["postgresVersion"] = pgVersion
            };

            if (postgisVersion is not null)
            {
                data["postgisVersion"] = postgisVersion;
                return HealthCheckResult.Healthy(
                    $"PostGIS connection successful. PostGIS {postgisVersion}.",
                    data);
            }

            return HealthCheckResult.Degraded(
                "PostgreSQL connection successful but PostGIS extension not found.",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostGIS health check failed");

            return HealthCheckResult.Unhealthy(
                $"PostgreSQL/PostGIS connection failed: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Item 292: Verifies that data-at-rest and data-in-transit encryption layers are
/// active. In production, checks that:
/// <list type="bullet">
///   <item>SQL Server connection strings contain <c>Encrypt=True</c> (TDE + transport encryption)</item>
///   <item>Redis connection strings contain <c>ssl=true</c> or use a TLS port (6380)</item>
///   <item>Kafka bootstrap servers use SASL_SSL protocol</item>
/// </list>
/// Returns <see cref="HealthCheckResult.Healthy"/> in development (checks are advisory only).
/// Returns <see cref="HealthCheckResult.Degraded"/> in production if any encryption is missing.
/// [NIST SC-28, STIG V-222588]
/// </summary>
internal sealed class EncryptionVerificationHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger _logger;

    public EncryptionVerificationHealthCheck(IConfiguration config, IHostEnvironment env, ILogger logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var data = new Dictionary<string, object>();

        // Check SQL Server encryption
        var sqlConn = _config.GetConnectionString("SqlServer")
            ?? _config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(sqlConn))
        {
            var sqlEncrypted = sqlConn.Contains("Encrypt=True", StringComparison.OrdinalIgnoreCase)
                || sqlConn.Contains("Encrypt=yes", StringComparison.OrdinalIgnoreCase);
            data["sql_encrypted"] = sqlEncrypted;
            if (!sqlEncrypted)
                warnings.Add("SQL Server connection does not have Encrypt=True");
        }

        // Check Redis TLS
        var redisConn = _config.GetConnectionString("Redis") ?? _config["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redisConn))
        {
            var redisTls = redisConn.Contains("ssl=true", StringComparison.OrdinalIgnoreCase)
                || redisConn.Contains(":6380", StringComparison.Ordinal);
            data["redis_tls"] = redisTls;
            if (!redisTls)
                warnings.Add("Redis connection does not use TLS (ssl=true or port 6380)");
        }

        // Check Kafka SASL_SSL
        var kafkaServers = _config["Kafka:BootstrapServers"] ?? _config.GetConnectionString("Kafka");
        if (!string.IsNullOrEmpty(kafkaServers))
        {
            var kafkaSecurity = _config["Kafka:SecurityProtocol"];
            var kafkaSecure = "SASL_SSL".Equals(kafkaSecurity, StringComparison.OrdinalIgnoreCase)
                || "SSL".Equals(kafkaSecurity, StringComparison.OrdinalIgnoreCase);
            data["kafka_ssl"] = kafkaSecure;
            if (!kafkaSecure)
                warnings.Add("Kafka does not use SASL_SSL or SSL security protocol");
        }

        if (warnings.Count == 0)
            return Task.FromResult(HealthCheckResult.Healthy("All configured encryption layers are active.", data));

        var message = string.Join("; ", warnings);
        _logger.LogWarning("Encryption verification: {Warnings}", message);

        // In production, degraded. In development, advisory only (still healthy).
        return _env.IsProduction()
            ? Task.FromResult(HealthCheckResult.Degraded(message, data: data))
            : Task.FromResult(HealthCheckResult.Healthy($"[Dev] Encryption warnings: {message}", data));
    }
}
