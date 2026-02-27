using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Application layer: CQRS commands, queries, behaviors, interfaces.
/// </summary>
public sealed class ApplicationProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ApplicationProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Application";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // DI extension
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "DependencyInjection.cs",
            _engine.Render(Templates.DependencyInjection, new { Service = service }),
            ct);

        // Common interfaces
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Interfaces", "IUnitOfWork.cs"),
            _engine.Render(Templates.IUnitOfWork, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Interfaces", "IRepository.cs"),
            _engine.Render(Templates.IRepository, new { Service = service }),
            ct);

        // MediatR behaviors
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Behaviors", "ValidationBehavior.cs"),
            _engine.Render(Templates.ValidationBehavior, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Behaviors", "LoggingBehavior.cs"),
            _engine.Render(Templates.LoggingBehavior, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Behaviors", "PerformanceBehavior.cs"),
            _engine.Render(Templates.PerformanceBehavior, new { Service = service }),
            ct);

        // Generate commands and queries per operation
        var commandCount = service.Operations.Count(o => o.IsCommand);
        var queryCount = service.Operations.Count(o => !o.IsCommand);
        _logger.LogDebug("    Generating {Commands} Commands and {Queries} Queries...", commandCount, queryCount);

        foreach (var op in service.Operations)
        {
            if (op.IsCommand)
            {
                await emitter.EmitToProjectAsync(serviceRoot, projectName,
                    Path.Combine("Commands", $"{op.PascalOperationId}Command.cs"),
                    _engine.Render(Templates.Command, new { Service = service, Operation = op }),
                    ct);

                await emitter.EmitToProjectAsync(serviceRoot, projectName,
                    Path.Combine("Commands", $"{op.PascalOperationId}CommandHandler.cs"),
                    _engine.Render(Templates.CommandHandler, new { Service = service, Operation = op }),
                    ct);

                await emitter.EmitToProjectAsync(serviceRoot, projectName,
                    Path.Combine("Commands", $"{op.PascalOperationId}CommandValidator.cs"),
                    _engine.Render(Templates.CommandValidator, new { Service = service, Operation = op }),
                    ct);
            }
            else
            {
                await emitter.EmitToProjectAsync(serviceRoot, projectName,
                    Path.Combine("Queries", $"{op.PascalOperationId}Query.cs"),
                    _engine.Render(Templates.Query, new { Service = service, Operation = op }),
                    ct);

                await emitter.EmitToProjectAsync(serviceRoot, projectName,
                    Path.Combine("Queries", $"{op.PascalOperationId}QueryHandler.cs"),
                    _engine.Render(Templates.QueryHandler, new { Service = service, Operation = op }),
                    ct);
            }
        }

        // DTOs from response schemas
        foreach (var schema in service.Schemas.Where(s => !s.IsEntity && !s.IsEnum))
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("DTOs", $"{schema.PascalName}Dto.cs"),
                _engine.Render(Templates.Dto, new { Service = service, Schema = schema }),
                ct);
        }

        // Mapping profile
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Mappings", "ServiceMappingProfile.cs"),
            _engine.Render(Templates.MappingProfile, new { Service = service }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <RootNamespace>{{ Service.PascalName }}.Application</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="MediatR" Version="12.*" />
                <PackageReference Include="FluentValidation" Version="11.*" />
                <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
                <PackageReference Include="AutoMapper" Version="13.*" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Domain\{{ Service.PascalName }}.Domain.csproj" />
              </ItemGroup>
            </Project>
            """;

        public const string DependencyInjection = """
            using System.Reflection;
            using {{ Service.PascalName }}.Application.Behaviors;
            using FluentValidation;
            using MediatR;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{ Service.PascalName }}.Application;

            public static class DependencyInjection
            {
                public static IServiceCollection AddApplicationServices(this IServiceCollection services)
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
                    services.AddValidatorsFromAssembly(assembly);
                    services.AddAutoMapper(assembly);

                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

                    return services;
                }
            }
            """;

        public const string IUnitOfWork = """
            namespace {{ Service.PascalName }}.Application.Interfaces;

            public interface IUnitOfWork : IDisposable
            {
                Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
                Task BeginTransactionAsync(CancellationToken cancellationToken = default);
                Task CommitTransactionAsync(CancellationToken cancellationToken = default);
                Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
            }
            """;

        public const string IRepository = """
            using System.Linq.Expressions;

            namespace {{ Service.PascalName }}.Application.Interfaces;

            public interface IRepository<TEntity> where TEntity : class
            {
                Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
                Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
                Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
                Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
                Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
                Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
                Task<int> CountAsync(CancellationToken cancellationToken = default);
                Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
            }
            """;

        public const string ValidationBehavior = """
            using FluentValidation;
            using MediatR;

            namespace {{ Service.PascalName }}.Application.Behaviors;

            public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                private readonly IEnumerable<IValidator<TRequest>> _validators;

                public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
                    => _validators = validators;

                public async Task<TResponse> Handle(
                    TRequest request,
                    RequestHandlerDelegate<TResponse> next,
                    CancellationToken cancellationToken)
                {
                    if (!_validators.Any())
                        return await next();

                    var context = new ValidationContext<TRequest>(request);
                    var results = await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                    var failures = results
                        .SelectMany(r => r.Errors)
                        .Where(f => f is not null)
                        .ToList();

                    if (failures.Count > 0)
                        throw new ValidationException(failures);

                    return await next();
                }
            }
            """;

        public const string LoggingBehavior = """
            using MediatR;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Application.Behaviors;

            public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

                public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
                    => _logger = logger;

                public async Task<TResponse> Handle(
                    TRequest request,
                    RequestHandlerDelegate<TResponse> next,
                    CancellationToken cancellationToken)
                {
                    var requestName = typeof(TRequest).Name;
                    _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

                    var response = await next();

                    _logger.LogInformation("Handled {RequestName}: {@Response}", requestName, response);
                    return response;
                }
            }
            """;

        public const string PerformanceBehavior = """
            using System.Diagnostics;
            using MediatR;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Application.Behaviors;

            public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
                private const int ThresholdMs = 500;

                public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
                    => _logger = logger;

                public async Task<TResponse> Handle(
                    TRequest request,
                    RequestHandlerDelegate<TResponse> next,
                    CancellationToken cancellationToken)
                {
                    var sw = Stopwatch.StartNew();
                    var response = await next();
                    sw.Stop();

                    if (sw.ElapsedMilliseconds > ThresholdMs)
                    {
                        _logger.LogWarning(
                            "Long running request: {RequestName} ({ElapsedMs}ms) {@Request}",
                            typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
                    }

                    return response;
                }
            }
            """;

        public const string Command = """
            using MediatR;

            namespace {{ Service.PascalName }}.Application.Commands;

            /// <summary>
            /// {{ Operation.Summary }}
            /// </summary>
            public sealed record {{ Operation.PascalOperationId }}Command : IRequest<{{ Operation.PascalOperationId }}CommandResult>
            {
            {{~ for p in Operation.Parameters ~}}
                public {{ p.CSharpType }}{{ if !p.Required }}?{{ end }} {{ p.PascalName }} { get; init; }
            {{~ end ~}}
            {{~ for p in Operation.RequestBodyProperties ~}}
                public {{ p.CSharpType }}{{ if !p.Required }}?{{ end }} {{ p.PascalName }} { get; init; }
            {{~ end ~}}
            }

            public sealed record {{ Operation.PascalOperationId }}CommandResult
            {
                public bool Success { get; init; }
                public string? Id { get; init; }
                public string? Message { get; init; }
            }
            """;

        public const string CommandHandler = """
            using MediatR;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Application.Commands;

            public sealed class {{ Operation.PascalOperationId }}CommandHandler
                : IRequestHandler<{{ Operation.PascalOperationId }}Command, {{ Operation.PascalOperationId }}CommandResult>
            {
                private readonly ILogger<{{ Operation.PascalOperationId }}CommandHandler> _logger;

                public {{ Operation.PascalOperationId }}CommandHandler(
                    ILogger<{{ Operation.PascalOperationId }}CommandHandler> logger)
                {
                    _logger = logger;
                }

                public async Task<{{ Operation.PascalOperationId }}CommandResult> Handle(
                    {{ Operation.PascalOperationId }}Command request,
                    CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Handling {{ Operation.PascalOperationId }} command");

                    // TODO: Implement command logic
                    await Task.CompletedTask;

                    return new {{ Operation.PascalOperationId }}CommandResult
                    {
                        Success = true,
                        Message = "{{ Operation.PascalOperationId }} completed successfully"
                    };
                }
            }
            """;

        public const string CommandValidator = """
            using FluentValidation;

            namespace {{ Service.PascalName }}.Application.Commands;

            public sealed class {{ Operation.PascalOperationId }}CommandValidator
                : AbstractValidator<{{ Operation.PascalOperationId }}Command>
            {
                public {{ Operation.PascalOperationId }}CommandValidator()
                {
            {{~ for p in Operation.Parameters ~}}
            {{~ if p.Required ~}}
                    RuleFor(x => x.{{ p.PascalName }}).NotEmpty()
                        .WithMessage("{{ p.PascalName }} is required");
            {{~ end ~}}
            {{~ end ~}}
            {{~ for p in Operation.RequestBodyProperties ~}}
            {{~ if p.Required ~}}
                    RuleFor(x => x.{{ p.PascalName }}).NotEmpty()
                        .WithMessage("{{ p.PascalName }} is required");
            {{~ end ~}}
            {{~ end ~}}
                }
            }
            """;

        public const string Query = """
            using MediatR;

            namespace {{ Service.PascalName }}.Application.Queries;

            /// <summary>
            /// {{ Operation.Summary }}
            /// </summary>
            public sealed record {{ Operation.PascalOperationId }}Query : IRequest<{{ Operation.PascalOperationId }}QueryResult>
            {
            {{~ for p in Operation.Parameters ~}}
                public {{ p.CSharpType }}{{ if !p.Required }}?{{ end }} {{ p.PascalName }} { get; init; }
            {{~ end ~}}
            }

            public sealed record {{ Operation.PascalOperationId }}QueryResult
            {
                public object? Data { get; init; }
                public int TotalCount { get; init; }
            }
            """;

        public const string QueryHandler = """
            using MediatR;
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Application.Queries;

            public sealed class {{ Operation.PascalOperationId }}QueryHandler
                : IRequestHandler<{{ Operation.PascalOperationId }}Query, {{ Operation.PascalOperationId }}QueryResult>
            {
                private readonly ILogger<{{ Operation.PascalOperationId }}QueryHandler> _logger;

                public {{ Operation.PascalOperationId }}QueryHandler(
                    ILogger<{{ Operation.PascalOperationId }}QueryHandler> logger)
                {
                    _logger = logger;
                }

                public async Task<{{ Operation.PascalOperationId }}QueryResult> Handle(
                    {{ Operation.PascalOperationId }}Query request,
                    CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Handling {{ Operation.PascalOperationId }} query");

                    // TODO: Implement query logic
                    await Task.CompletedTask;

                    return new {{ Operation.PascalOperationId }}QueryResult
                    {
                        Data = null,
                        TotalCount = 0
                    };
                }
            }
            """;

        public const string Dto = """
            namespace {{ Service.PascalName }}.Application.DTOs;

            /// <summary>
            /// {{ Schema.Description }}
            /// </summary>
            public sealed record {{ Schema.PascalName }}Dto
            {
            {{~ for prop in Schema.Properties ~}}
                /// <summary>{{ prop.Description }}</summary>
                public {{ prop.CSharpType }}{{ if !prop.Required }}?{{ end }} {{ prop.PascalName }} { get; init; }
            {{~ end ~}}
            }
            """;

        public const string MappingProfile = """
            using AutoMapper;

            namespace {{ Service.PascalName }}.Application.Mappings;

            public class ServiceMappingProfile : Profile
            {
                public ServiceMappingProfile()
                {
            {{~ for schema in Service.Schemas ~}}
            {{~ if schema.IsEntity ~}}
                    // TODO: Map {{ schema.PascalName }} → {{ schema.PascalName }}Dto
                    // CreateMap<Domain.Entities.{{ schema.PascalName }}, DTOs.{{ schema.PascalName }}Dto>();
            {{~ end ~}}
            {{~ end ~}}
                }
            }
            """;
    }
}
