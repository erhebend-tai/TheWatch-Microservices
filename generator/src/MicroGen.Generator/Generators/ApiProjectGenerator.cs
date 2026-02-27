using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the API project: controllers, middleware, Program.cs, project file.
/// </summary>
public sealed class ApiProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ApiProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Api";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // Program.cs
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Program.cs",
            _engine.Render(Templates.ProgramCs, new { Service = service, Config = _config }),
            ct);

        // appsettings.json
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "appsettings.json",
            _engine.Render(Templates.AppSettings, new { Service = service, Config = _config }),
            ct);

        // appsettings.Development.json
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "appsettings.Development.json",
            _engine.Render(Templates.AppSettingsDev, new { Service = service }),
            ct);

        // Controllers — one per tag
        var tagGroups = service.Operations.GroupBy(o => o.Tag).ToList();
        _logger.LogDebug("    Generating {Count} API Controllers...", tagGroups.Count);

        foreach (var group in tagGroups)
        {
            var tagName = group.Key ?? "Default";
            var tag = service.Tags.FirstOrDefault(t =>
                t.Name != null && t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                ?? new TagDescriptor { Name = tagName };

            var operations = group.ToList();

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Controllers", $"{tag.PascalName}Controller.cs"),
                _engine.Render(Templates.Controller, new
                {
                    Service = service,
                    Tag = tag,
                    Operations = operations
                }),
                ct);
        }

        // Middleware
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Middleware", "ExceptionHandlingMiddleware.cs"),
            _engine.Render(Templates.ExceptionMiddleware, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Middleware", "CorrelationIdMiddleware.cs"),
            _engine.Render(Templates.CorrelationMiddleware, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Middleware", "RequestLoggingMiddleware.cs"),
            _engine.Render(Templates.RequestLoggingMiddleware, new { Service = service }),
            ct);

        // Extensions
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Extensions", "ServiceCollectionExtensions.cs"),
            _engine.Render(Templates.ServiceCollectionExtensions, new { Service = service, Config = _config }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <AnalysisLevel>latest-all</AnalysisLevel>
                <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
                <RootNamespace>{{ Service.PascalName }}.Api</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="MediatR" Version="12.*" />
                <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
                <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
                <PackageReference Include="Asp.Versioning.Mvc" Version="8.*" />
                <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.*" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
                <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
            {{~ if Config.Features.Dapr ~}}
                <PackageReference Include="Dapr.AspNetCore" Version="1.*" />
            {{~ end ~}}
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Application\{{ Service.PascalName }}.Application.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Infrastructure\{{ Service.PascalName }}.Infrastructure.csproj" />
            {{~ if Config.Features.AspireAppHost ~}}
                <ProjectReference Include="..\..\..\{{ pascal_case Service.DomainName }}.Aspire.ServiceDefaults\{{ pascal_case Service.DomainName }}.Aspire.ServiceDefaults.csproj" />
            {{~ end ~}}
              </ItemGroup>
            </Project>
            """;

        public const string ProgramCs = """
            using {{ Service.PascalName }}.Api.Middleware;
            using {{ Service.PascalName }}.Application;
            using {{ Service.PascalName }}.Infrastructure;
            using Serilog;
            {{~ if Config.Features.AspireAppHost ~}}
            using {{ pascal_case Service.DomainName }}.Aspire.ServiceDefaults;
            {{~ end ~}}
            {{~ if Config.Features.Dapr ~}}
            using {{ Service.PascalName }}.Api.Services;
            {{~ end ~}}

            var builder = WebApplication.CreateBuilder(args);
            {{~ if Config.Features.AspireAppHost ~}}

            builder.AddServiceDefaults();
            {{~ end ~}}

            // Serilog
            builder.Host.UseSerilog((context, services, config) => config
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("ServiceName", "{{ Service.PascalName }}"));

            // Application layer (MediatR, FluentValidation, AutoMapper)
            builder.Services.AddApplicationServices();

            // Infrastructure layer (EF Core, repositories, caching, telemetry)
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // API layer
            builder.Services.AddControllers(){{~ if Config.Features.Dapr ~}}.AddDapr(){{~ end ~}};
            builder.Services.AddEndpointsApiExplorer();
            {{~ if Config.Features.Dapr ~}}

            // Dapr
            builder.Services.AddDaprServices();
            {{~ end ~}}
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "{{ Service.Title }}",
                    Version = "{{ Service.Version }}",
                    Description = "{{ Service.Description }}"
                });
            });

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            builder.Services.AddHealthChecks();

            var app = builder.Build();
            {{~ if Config.Features.AspireAppHost ~}}

            app.MapDefaultEndpoints();
            {{~ end ~}}

            // Middleware pipeline
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            {{~ if Config.Features.Dapr ~}}
            app.UseCloudEvents();
            {{~ end ~}}
            app.UseAuthentication();
            app.UseAuthorization();
            {{~ if Config.Features.Dapr && Config.Dapr.VoiceEndpoints ~}}

            app.UseWebSockets();
            {{~ end ~}}

            app.MapControllers();
            {{~ if Config.Features.Dapr ~}}
            app.MapSubscribeHandler();
            {{~ end ~}}
            app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
            app.MapHealthChecks("/health/ready");
            {{~ if Config.Features.Dapr && Config.Dapr.VoiceEndpoints ~}}
            app.Map("/ws/ask", {{ Service.PascalName }}.Api.Voice.VoiceWebSocketHandler.Handle);
            {{~ end ~}}

            app.Run();
            """;

        public const string Controller = """
            using {{ Service.PascalName }}.Application.Commands;
            using {{ Service.PascalName }}.Application.Queries;
            using MediatR;
            using Microsoft.AspNetCore.Authorization;
            using Microsoft.AspNetCore.Mvc;

            namespace {{ Service.PascalName }}.Api.Controllers;

            /// <summary>
            /// {{ Tag.Description }}
            /// </summary>
            [ApiController]
            [Route("api/v{version:apiVersion}/[controller]")]
            [Produces("application/json")]
            [ApiVersion("1.0")]
            public class {{ Tag.PascalName }}Controller : ControllerBase
            {
                private readonly IMediator _mediator;
                private readonly ILogger<{{ Tag.PascalName }}Controller> _logger;

                public {{ Tag.PascalName }}Controller(IMediator mediator, ILogger<{{ Tag.PascalName }}Controller> logger)
                {
                    _mediator = mediator;
                    _logger = logger;
                }
            {{~ for op in Operations ~}}

                /// <summary>
                /// {{ op.Summary }}
                /// </summary>
                /// <remarks>{{ op.Description }}</remarks>
                [Http{{ op.HttpMethod | string.capitalize }}("{{ op.Path }}")]
                [ProducesResponseType(StatusCodes.Status200OK)]
                [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
                [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
                public async Task<IActionResult> {{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                    [FromRoute] {{ p.CSharpType }} {{ p.CamelName }},
            {{~ end ~}}
            {{~ if op.HasRequestBody ~}}
                    [FromBody] {{ op.PascalOperationId }}{{ if op.IsCommand }}Command{{ else }}Query{{ end }} request,
            {{~ end ~}}
                    CancellationToken cancellationToken = default)
                {
                    _logger.LogInformation("Executing {{ op.PascalOperationId }}");
            {{~ if op.IsQuery ~}}
                    var query = new {{ op.PascalOperationId }}Query
                    {
            {{~ for p in op.PathParameters ~}}
                        {{ p.PascalName }} = {{ p.CamelName }},
            {{~ end ~}}
                    };
                    var result = await _mediator.Send(query, cancellationToken);
                    return Ok(result);
            {{~ else ~}}
                    var command = new {{ op.PascalOperationId }}Command
                    {
            {{~ for p in op.PathParameters ~}}
                        {{ p.PascalName }} = {{ p.CamelName }},
            {{~ end ~}}
                    };
                    var result = await _mediator.Send(command, cancellationToken);
                    return {{ if op.HttpMethod == "POST" }}CreatedAtAction(nameof({{ op.PascalOperationId }}Async), result){{ else }}Ok(result){{ end }};
            {{~ end ~}}
                }
            {{~ end ~}}
            }
            """;

        public const string ExceptionMiddleware = """
            using System.Net;
            using System.Text.Json;
            using Microsoft.AspNetCore.Mvc;

            namespace {{ Service.PascalName }}.Api.Middleware;

            public class ExceptionHandlingMiddleware
            {
                private readonly RequestDelegate _next;
                private readonly ILogger<ExceptionHandlingMiddleware> _logger;

                public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
                {
                    _next = next;
                    _logger = logger;
                }

                public async Task InvokeAsync(HttpContext context)
                {
                    try
                    {
                        await _next(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
                        await HandleExceptionAsync(context, ex);
                    }
                }

                private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
                {
                    var (statusCode, title) = exception switch
                    {
                        ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
                        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
                    };

                    var problem = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = exception.Message,
                        Instance = context.Request.Path
                    };

                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                }
            }
            """;

        public const string CorrelationMiddleware = """
            namespace {{ Service.PascalName }}.Api.Middleware;

            public class CorrelationIdMiddleware
            {
                private const string CorrelationIdHeader = "X-Correlation-ID";
                private readonly RequestDelegate _next;

                public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

                public async Task InvokeAsync(HttpContext context)
                {
                    if (!context.Request.Headers.ContainsKey(CorrelationIdHeader))
                    {
                        context.Request.Headers[CorrelationIdHeader] = Guid.NewGuid().ToString();
                    }

                    var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers[CorrelationIdHeader] = correlationId;
                        return Task.CompletedTask;
                    });

                    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
                    {
                        await _next(context);
                    }
                }
            }
            """;

        public const string RequestLoggingMiddleware = """
            using System.Diagnostics;

            namespace {{ Service.PascalName }}.Api.Middleware;

            public class RequestLoggingMiddleware
            {
                private readonly RequestDelegate _next;
                private readonly ILogger<RequestLoggingMiddleware> _logger;

                public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
                {
                    _next = next;
                    _logger = logger;
                }

                public async Task InvokeAsync(HttpContext context)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await _next(context);
                        sw.Stop();
                        _logger.LogInformation(
                            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                            context.Request.Method, context.Request.Path,
                            context.Response.StatusCode, sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.LogError(ex,
                            "HTTP {Method} {Path} failed after {ElapsedMs}ms",
                            context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
                        throw;
                    }
                }
            }
            """;

        public const string ServiceCollectionExtensions = """
            namespace {{ Service.PascalName }}.Api.Extensions;

            public static class ServiceCollectionExtensions
            {
                public static IServiceCollection AddApiServices(this IServiceCollection services)
                {
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                    return services;
                }
            }
            """;

        public const string AppSettings = """
            {
              "ServiceName": "{{ Service.PascalName }}",
              "ServiceVersion": "{{ Service.Version }}",
              "ConnectionStrings": {
                "DefaultConnection": "Host=localhost;Database={{ Service.KebabName }};Username=app;Password=secret",
                "Redis": "localhost:6379"
              },
              "Serilog": {
                "MinimumLevel": {
                  "Default": "Information",
                  "Override": {
                    "Microsoft": "Warning",
                    "System": "Warning",
                    "Microsoft.EntityFrameworkCore": "Warning"
                  }
                },
                "WriteTo": [
                  { "Name": "Console" }
                ],
                "Enrich": ["FromLogContext", "WithMachineName"]
              },
              "AllowedHosts": "*"
            }
            """;

        public const string AppSettingsDev = """
            {
              "Serilog": {
                "MinimumLevel": {
                  "Default": "Debug"
                }
              }
            }
            """;
    }
}
