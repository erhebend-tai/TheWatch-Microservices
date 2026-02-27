using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Domain layer: entities, value objects, events, enums, exceptions.
/// </summary>
public sealed class DomainProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public DomainProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Domain";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // Base entity
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Common", "BaseEntity.cs"),
            _engine.Render(Templates.BaseEntity, new { Service = service }),
            ct);

        // Base auditable entity
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Common", "BaseAuditableEntity.cs"),
            _engine.Render(Templates.AuditableEntity, new { Service = service }),
            ct);

        // Domain event base
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Common", "BaseDomainEvent.cs"),
            _engine.Render(Templates.BaseDomainEvent, new { Service = service }),
            ct);

        // Value objects
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("ValueObjects", "GeoLocation.cs"),
            _engine.Render(Templates.GeoLocation, new { Service = service }),
            ct);

        // Entities from schemas
        var entities = service.Schemas.Where(s => s.IsEntity).ToList();
        _logger.LogDebug("    Generating {Count} Entities...", entities.Count);

        foreach (var schema in entities)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Entities", $"{schema.PascalName}.cs"),
                _engine.Render(Templates.Entity, new { Service = service, Schema = schema }),
                ct);

            // Domain event for entity
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Events", $"{schema.PascalName}CreatedEvent.cs"),
                _engine.Render(Templates.DomainEvent, new
                {
                    Service = service,
                    Schema = schema,
                    EventName = $"{schema.PascalName}Created"
                }),
                ct);

            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Events", $"{schema.PascalName}UpdatedEvent.cs"),
                _engine.Render(Templates.DomainEvent, new
                {
                    Service = service,
                    Schema = schema,
                    EventName = $"{schema.PascalName}Updated"
                }),
                ct);
        }

        // Enums from schemas
        var enums = service.Schemas.Where(s => s.IsEnum).ToList();
        _logger.LogDebug("    Generating {Count} Enums...", enums.Count);

        foreach (var schema in enums)
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Enums", $"{schema.PascalName}.cs"),
                _engine.Render(Templates.Enum, new { Service = service, Schema = schema }),
                ct);
        }

        // Domain exceptions
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Exceptions", "DomainException.cs"),
            _engine.Render(Templates.DomainException, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Exceptions", "NotFoundException.cs"),
            _engine.Render(Templates.NotFoundException, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Exceptions", "ConflictException.cs"),
            _engine.Render(Templates.ConflictException, new { Service = service }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <RootNamespace>{{ Service.PascalName }}.Domain</RootNamespace>
              </PropertyGroup>
            </Project>
            """;

        public const string BaseEntity = """
            namespace {{ Service.PascalName }}.Domain.Common;

            public abstract class BaseEntity
            {
                public Guid Id { get; set; } = Guid.NewGuid();

                private readonly List<BaseDomainEvent> _domainEvents = [];

                public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

                public void AddDomainEvent(BaseDomainEvent domainEvent)
                    => _domainEvents.Add(domainEvent);

                public void RemoveDomainEvent(BaseDomainEvent domainEvent)
                    => _domainEvents.Remove(domainEvent);

                public void ClearDomainEvents()
                    => _domainEvents.Clear();
            }
            """;

        public const string AuditableEntity = """
            namespace {{ Service.PascalName }}.Domain.Common;

            public abstract class BaseAuditableEntity : BaseEntity
            {
                public DateTimeOffset CreatedAt { get; set; }
                public string? CreatedBy { get; set; }
                public DateTimeOffset? LastModifiedAt { get; set; }
                public string? LastModifiedBy { get; set; }
            }
            """;

        public const string BaseDomainEvent = """
            using MediatR;

            namespace {{ Service.PascalName }}.Domain.Common;

            public abstract class BaseDomainEvent : INotification
            {
                public Guid EventId { get; } = Guid.NewGuid();
                public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
            }
            """;

        public const string GeoLocation = """
            namespace {{ Service.PascalName }}.Domain.ValueObjects;

            public sealed record GeoLocation
            {
                public double Latitude { get; init; }
                public double Longitude { get; init; }
                public double? Altitude { get; init; }
                public double? Accuracy { get; init; }

                public GeoLocation(double latitude, double longitude)
                {
                    if (latitude is < -90 or > 90)
                        throw new ArgumentOutOfRangeException(nameof(latitude));
                    if (longitude is < -180 or > 180)
                        throw new ArgumentOutOfRangeException(nameof(longitude));

                    Latitude = latitude;
                    Longitude = longitude;
                }

                public double DistanceTo(GeoLocation other)
                {
                    const double R = 6371e3; // metres
                    var φ1 = Latitude * Math.PI / 180;
                    var φ2 = other.Latitude * Math.PI / 180;
                    var Δφ = (other.Latitude - Latitude) * Math.PI / 180;
                    var Δλ = (other.Longitude - Longitude) * Math.PI / 180;

                    var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                            Math.Cos(φ1) * Math.Cos(φ2) *
                            Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

                    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                    return R * c;
                }
            }
            """;

        public const string Entity = """
            using {{ Service.PascalName }}.Domain.Common;

            namespace {{ Service.PascalName }}.Domain.Entities;

            /// <summary>
            /// {{ Schema.Description }}
            /// </summary>
            public class {{ Schema.PascalName }} : BaseAuditableEntity
            {
            {{~ for prop in Schema.Properties ~}}
            {{~ if prop.Name != "id" && prop.Name != "createdAt" && prop.Name != "updatedAt" ~}}
                /// <summary>{{ prop.Description }}</summary>
                public {{ prop.CSharpType }}{{ if !prop.Required }}?{{ end }} {{ prop.PascalName }} { get; set; }
            {{~ end ~}}
            {{~ end ~}}
            }
            """;

        public const string DomainEvent = """
            using {{ Service.PascalName }}.Domain.Common;

            namespace {{ Service.PascalName }}.Domain.Events;

            public sealed class {{ EventName }}Event : BaseDomainEvent
            {
                public Guid EntityId { get; }

                public {{ EventName }}Event(Guid entityId)
                {
                    EntityId = entityId;
                }
            }
            """;

        public const string Enum = """
            namespace {{ Service.PascalName }}.Domain.Enums;

            /// <summary>
            /// {{ Schema.Description }}
            /// </summary>
            public enum {{ Schema.PascalName }}
            {
            {{~ for val in Schema.EnumValues ~}}
                {{ val | string.capitalize }},
            {{~ end ~}}
            }
            """;

        public const string DomainException = """
            namespace {{ Service.PascalName }}.Domain.Exceptions;

            public class DomainException : Exception
            {
                public DomainException() { }
                public DomainException(string message) : base(message) { }
                public DomainException(string message, Exception innerException) : base(message, innerException) { }
            }
            """;

        public const string NotFoundException = """
            namespace {{ Service.PascalName }}.Domain.Exceptions;

            public class NotFoundException : DomainException
            {
                public NotFoundException(string name, object key)
                    : base($"Entity \"{name}\" ({key}) was not found.") { }
            }
            """;

        public const string ConflictException = """
            namespace {{ Service.PascalName }}.Domain.Exceptions;

            public class ConflictException : DomainException
            {
                public ConflictException(string message) : base(message) { }
            }
            """;
    }
}
