using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Aspire AppHost project for a domain.
/// Orchestrates all services within the domain plus shared infrastructure (Redis, PostgreSQL).
/// One per domain.
/// </summary>
public sealed class AspireAppHostGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public AspireAppHostGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateDomainAppHostAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{domain.PascalName}.Aspire.AppHost";
        var projectRoot = Path.Combine(domainRoot, projectName);
        var model = new { Domain = domain, Config = _config };

        _logger.LogDebug("  Generating Aspire AppHost for domain {Domain}...", domain.DomainName);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, $"{projectName}.csproj"),
            _engine.Render(Templates.ProjectFile, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Program.cs"),
            _engine.Render(Templates.ProgramCs, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "appsettings.json"),
            _engine.Render(Templates.AppSettings, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "appsettings.Development.json"),
            _engine.Render(Templates.AppSettingsDev, model), ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <Sdk Name="Aspire.AppHost.Sdk" Version="9.*" />

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <IsAspireHost>true</IsAspireHost>
                <RootNamespace>{{ Domain.PascalName }}.Aspire.AppHost</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Aspire.Hosting.AppHost" Version="9.*" />
                <PackageReference Include="Aspire.Hosting.Redis" Version="9.*" />
                <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.*" />
            {{~ if Config.Features.Kafka ~}}
                <PackageReference Include="Aspire.Hosting.Kafka" Version="9.*" />
            {{~ end ~}}
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Domain.PascalName }}.Aspire.ServiceDefaults\{{ Domain.PascalName }}.Aspire.ServiceDefaults.csproj" />
                <ProjectReference Include="..\{{ Domain.PascalName }}.Web\{{ Domain.PascalName }}.Web.csproj" />
            {{~ for svc in Domain.Services ~}}
                <ProjectReference Include="..\{{ svc.PascalName }}\src\{{ svc.PascalName }}.Api\{{ svc.PascalName }}.Api.csproj" />
            {{~ end ~}}
              </ItemGroup>
            </Project>
            """;

        public const string ProgramCs = """
            var builder = DistributedApplication.CreateBuilder(args);

            // ── Shared Infrastructure ────────────────────────────────────────
            var redis = builder.AddRedis("{{ Domain.KebabName }}-redis")
                .WithDataVolume();

            var postgres = builder.AddPostgres("{{ Domain.KebabName }}-postgres")
                .WithDataVolume()
                .WithPgAdmin();
            {{~ if Config.Features.Kafka ~}}

            var kafka = builder.AddKafka("{{ Domain.KebabName }}-kafka")
                .WithKafkaUI();
            {{~ end ~}}

            // Per-service databases
            {{~ for svc in Domain.Services ~}}
            var db{{ svc.PascalName }} = postgres.AddDatabase("{{ svc.KebabName }}-db");
            {{~ end ~}}

            // ── API Services ─────────────────────────────────────────────────
            {{~ for svc in Domain.Services ~}}
            var {{ svc.CamelName }}Api = builder.AddProject<Projects.{{ svc.PascalName }}_Api>("{{ svc.KebabName }}-api")
                .WithReference(redis)
                .WithReference(db{{ svc.PascalName }})
            {{~ if Config.Features.Kafka ~}}
                .WithReference(kafka)
            {{~ end ~}}
                .WithExternalHttpEndpoints();

            {{~ end ~}}
            // ── Web Frontend ─────────────────────────────────────────────────
            builder.AddProject<Projects.{{ Domain.PascalName }}_Web>("{{ Domain.KebabName }}-web")
            {{~ for svc in Domain.Services ~}}
                .WithReference({{ svc.CamelName }}Api)
            {{~ end ~}}
                .WithExternalHttpEndpoints();

            builder.Build().Run();
            """;

        public const string AppSettings = """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning",
                  "Aspire.Hosting": "Information"
                }
              }
            }
            """;

        public const string AppSettingsDev = """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning",
                  "Aspire.Hosting.Dcp": "Warning"
                }
              }
            }
            """;
    }
}
