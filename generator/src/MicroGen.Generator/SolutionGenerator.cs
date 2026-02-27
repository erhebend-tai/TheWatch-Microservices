using System.Diagnostics;
using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Generators;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator;

/// <summary>
/// Orchestrates the generation of a complete microservice solution from a ServiceDescriptor.
/// </summary>
public sealed class SolutionGenerator
{
    private readonly ILogger<SolutionGenerator> _logger;
    private readonly GeneratorConfig _config;
    private readonly TemplateEngine _templateEngine;

    public SolutionGenerator(
        ILogger<SolutionGenerator> logger,
        GeneratorConfig config)
    {
        _logger = logger;
        _config = config;
        _templateEngine = new TemplateEngine();
    }

    /// <summary>
    /// Generates a complete microservice solution for every service in the given domains,
    /// plus domain-level infrastructure (Nginx gateway, YARP API gateway).
    /// </summary>
    public async Task<GenerationSummary> GenerateAllAsync(
        List<DomainDescriptor> domains,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<GenerationResult>();

        foreach (var domain in domains)
        {
            // Per-service generation
            foreach (var service in domain.Services)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await GenerateServiceAsync(service, cancellationToken);
                results.Add(result);
            }

            // Domain-level gateway generation
            cancellationToken.ThrowIfCancellationRequested();
            await GenerateDomainGatewayAsync(domain, cancellationToken);
        }

        sw.Stop();
        return new GenerationSummary
        {
            Results = results,
            TotalDuration = sw.Elapsed
        };
    }

    /// <summary>
    /// Generates domain-level infrastructure: Nginx gateway and YARP API gateway.
    /// </summary>
    public async Task GenerateDomainGatewayAsync(
        DomainDescriptor domain,
        CancellationToken cancellationToken = default)
    {
        if (domain.Services.Count == 0) return;

        var domainRoot = Path.Combine(
            _config.OutputDirectory,
            _config.OutputStructure == "domain-grouped" ? domain.Name : "");
        domainRoot = Path.GetFullPath(domainRoot);

        var emitter = new FileEmitter(_config.DryRun);

        try
        {
            // Nginx reverse proxy configuration
            var nginxGen = new NginxGenerator(_templateEngine, _config, _logger);
            await nginxGen.GenerateDomainGatewayAsync(domain, domainRoot, emitter, cancellationToken);

            // YARP API Gateway project
            var gatewayGen = new ApiGatewayGenerator(_templateEngine, _config, _logger);
            await gatewayGen.GenerateDomainGatewayAsync(domain, domainRoot, emitter, cancellationToken);

            // Aspire projects (domain-level)
            if (_config.Features.AspireAppHost)
            {
                var serviceDefaultsGen = new AspireServiceDefaultsGenerator(_templateEngine, _config, _logger);
                await serviceDefaultsGen.GenerateDomainServiceDefaultsAsync(domain, domainRoot, emitter, cancellationToken);

                var webGen = new AspireWebProjectGenerator(_templateEngine, _config, _logger);
                await webGen.GenerateDomainWebAsync(domain, domainRoot, emitter, cancellationToken);

                var appHostGen = new AspireAppHostGenerator(_templateEngine, _config, _logger);
                await appHostGen.GenerateDomainAppHostAsync(domain, domainRoot, emitter, cancellationToken);
            }

            // Apache Infrastructure (Kafka cluster, OpenWhisk platform, Dubbo registry)
            if (_config.Features.Kafka || _config.Features.OpenWhisk || _config.Features.Dubbo)
            {
                var apacheInfraGen = new ApacheInfrastructureGenerator(_templateEngine, _config, _logger);
                await apacheInfraGen.GenerateDomainInfrastructureAsync(domain, domainRoot, emitter, cancellationToken);
            }

            // Apache Analytics (Superset, ECharts dashboard)
            if (_config.Features.ECharts || _config.Features.Superset)
            {
                var analyticsGen = new ApacheAnalyticsGenerator(_templateEngine, _config, _logger);
                await analyticsGen.GenerateDomainAnalyticsAsync(domain, domainRoot, emitter, cancellationToken);
            }

            _logger.LogInformation("Generated domain-level projects for {Domain}: {Files} files",
                domain.Name, emitter.GeneratedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed generating domain gateway for {Domain}", domain.Name);
        }
    }

    /// <summary>
    /// Generates a complete solution for a single service.
    /// </summary>
    public async Task<GenerationResult> GenerateServiceAsync(
        ServiceDescriptor service,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var warnings = new List<string>();
        var errors = new List<string>();

        var serviceRoot = Path.Combine(
            _config.OutputDirectory,
            _config.OutputStructure == "domain-grouped"
                ? Path.Combine(service.DomainName, service.PascalName)
                : service.PascalName);

        serviceRoot = Path.GetFullPath(serviceRoot);

        _logger.LogInformation("Generating {Domain}/{Service} → {Output}",
            service.DomainName, service.PascalName, serviceRoot);

        var emitter = new FileEmitter(_config.DryRun);

        try
        {
            // 1. Solution file
            await GenerateSolutionFileAsync(service, serviceRoot, emitter, cancellationToken);

            // 2. Api project
            var apiGen = new ApiProjectGenerator(_templateEngine, _config, _logger);
            await apiGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 3. Application project
            var appGen = new ApplicationProjectGenerator(_templateEngine, _config, _logger);
            await appGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 4. Domain project
            var domainGen = new DomainProjectGenerator(_templateEngine, _config, _logger);
            await domainGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 5. Infrastructure project
            var infraGen = new InfrastructureProjectGenerator(_templateEngine, _config, _logger);
            await infraGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 6. Jobs project (if Hangfire enabled)
            if (_config.Features.Hangfire)
            {
                var jobsGen = new JobsProjectGenerator(_templateEngine, _config, _logger);
                await jobsGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
            }

            // 7. Dashboard project (if Blazor enabled)
            if (_config.Features.BlazorDashboard)
            {
                var dashGen = new DashboardProjectGenerator(_templateEngine, _config, _logger);
                await dashGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

                // 7b. Radzen CRUD pages (if RadzenUI enabled)
                if (_config.Features.RadzenUI)
                {
                    var radzenGen = new RadzenPageGenerator(_templateEngine, _config, _logger);
                    await radzenGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
                }
            }

            // 8. Test project
            if (!_config.SkipTests)
            {
                var testGen = new TestProjectGenerator(_templateEngine, _config, _logger);
                await testGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
            }

            // 9. Client SDK
            var clientGen = new ClientSdkProjectGenerator(_templateEngine, _config, _logger);
            await clientGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 10. Deployment artifacts
            var deployGen = new DeploymentGenerator(_templateEngine, _config, _logger);
            await deployGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 11. Nginx per-service config + K8s Ingress
            var nginxGen = new NginxGenerator(_templateEngine, _config, _logger);
            await nginxGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);

            // 12. Apache Infrastructure (Kafka topics, OpenWhisk actions, Dubbo configs)
            if (_config.Features.Kafka || _config.Features.OpenWhisk || _config.Features.Dubbo)
            {
                var apacheInfraGen = new ApacheInfrastructureGenerator(_templateEngine, _config, _logger);
                await apacheInfraGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
            }

            // 13. Apache Analytics (per-service metrics export config)
            if (_config.Features.ECharts || _config.Features.Superset)
            {
                var analyticsGen = new ApacheAnalyticsGenerator(_templateEngine, _config, _logger);
                await analyticsGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
            }

            // 14. Dapr sidecar infrastructure (components, services, controllers, deployment)
            if (_config.Features.Dapr)
            {
                // TODO: Implement DaprProjectGenerator
                // var daprGen = new DaprProjectGenerator(_templateEngine, _config, _logger);
                // await daprGen.GenerateAsync(service, serviceRoot, emitter, cancellationToken);
                _logger.LogWarning("Dapr generation is not yet implemented");
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            _logger.LogError(ex, "Failed generating {Service}", service.PascalName);
        }

        sw.Stop();

        return new GenerationResult
        {
            ServiceName = service.PascalName,
            DomainName = service.DomainName,
            OutputPath = serviceRoot,
            Success = errors.Count == 0,
            GeneratedFiles = emitter.GeneratedFiles.ToList(),
            Warnings = warnings,
            Errors = errors,
            Duration = sw.Elapsed
        };
    }

    private async Task GenerateSolutionFileAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken cancellationToken)
    {
        var sln = _templateEngine.Render(SolutionTemplates.SolutionFile, new
        {
            Service = service,
            Config = _config
        });

        await emitter.EmitAsync(
            Path.Combine(serviceRoot, $"{service.PascalName}.sln"),
            sln,
            cancellationToken);
    }
}

internal static class SolutionTemplates
{
    public const string SolutionFile = """
        Microsoft Visual Studio Solution File, Format Version 12.00
        # Visual Studio Version 17
        VisualStudioVersion = 17.12.0
        MinimumVisualStudioVersion = 10.0.40219.1
        Global
        	GlobalSection(SolutionConfigurationPlatforms) = preSolution
        		Debug|Any CPU = Debug|Any CPU
        		Release|Any CPU = Release|Any CPU
        	EndGlobalSection
        EndGlobal
        """;
}
