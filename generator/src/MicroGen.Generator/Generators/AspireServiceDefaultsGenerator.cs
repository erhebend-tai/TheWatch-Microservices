using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Aspire ServiceDefaults project for a domain.
/// Provides shared OpenTelemetry, health checks, service discovery, and HTTP resilience.
/// One per domain.
/// </summary>
public sealed class AspireServiceDefaultsGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public AspireServiceDefaultsGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateDomainServiceDefaultsAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{domain.PascalName}.Aspire.ServiceDefaults";
        var projectRoot = Path.Combine(domainRoot, projectName);
        var model = new { Domain = domain, Config = _config };

        _logger.LogDebug("  Generating Aspire ServiceDefaults for domain {Domain}...", domain.DomainName);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, $"{projectName}.csproj"),
            _engine.Render(Templates.ProjectFile, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Extensions.cs"),
            _engine.Render(Templates.ExtensionsCs, model), ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{{ Domain.PascalName }}.Aspire.ServiceDefaults</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
                <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
                <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.*" />
                <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
                <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />
              </ItemGroup>
            </Project>
            """;

        public const string ExtensionsCs = """
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Diagnostics.HealthChecks;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Diagnostics.HealthChecks;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;
            using OpenTelemetry;
            using OpenTelemetry.Metrics;
            using OpenTelemetry.Trace;

            namespace {{ Domain.PascalName }}.Aspire.ServiceDefaults;

            public static class Extensions
            {
                public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
                    where TBuilder : IHostApplicationBuilder
                {
                    builder.ConfigureOpenTelemetry();
                    builder.AddDefaultHealthChecks();

                    builder.Services.AddServiceDiscovery();
                    builder.Services.ConfigureHttpClientDefaults(http =>
                    {
                        http.AddStandardResilienceHandler();
                        http.AddServiceDiscovery();
                    });

                    return builder;
                }

                public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
                    where TBuilder : IHostApplicationBuilder
                {
                    builder.Logging.AddOpenTelemetry(logging =>
                    {
                        logging.IncludeFormattedMessage = true;
                        logging.IncludeScopes = true;
                    });

                    builder.Services.AddOpenTelemetry()
                        .WithMetrics(metrics =>
                        {
                            metrics.AddAspNetCoreInstrumentation()
                                .AddHttpClientInstrumentation()
                                .AddRuntimeInstrumentation();
                        })
                        .WithTracing(tracing =>
                        {
                            tracing.AddAspNetCoreInstrumentation()
                                .AddHttpClientInstrumentation();
                        });

                    builder.AddOpenTelemetryExporters();
                    return builder;
                }

                private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
                    where TBuilder : IHostApplicationBuilder
                {
                    var useOtlp = !string.IsNullOrWhiteSpace(
                        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

                    if (useOtlp)
                    {
                        builder.Services.AddOpenTelemetry().UseOtlpExporter();
                    }

                    return builder;
                }

                public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
                    where TBuilder : IHostApplicationBuilder
                {
                    builder.Services.AddHealthChecks()
                        .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

                    return builder;
                }

                public static WebApplication MapDefaultEndpoints(this WebApplication app)
                {
                    app.MapHealthChecks("/health/live", new HealthCheckOptions
                    {
                        Predicate = r => r.Tags.Contains("live")
                    });

                    app.MapHealthChecks("/health/ready");

                    app.MapHealthChecks("/alive", new HealthCheckOptions
                    {
                        Predicate = _ => false
                    });

                    return app;
                }
            }
            """;
    }
}
