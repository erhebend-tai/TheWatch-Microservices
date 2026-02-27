using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using TheWatch.Geospatial;
using TheWatch.Geospatial.Services;
using TheWatch.Geospatial.Spatial;
using Xunit;

namespace TheWatch.Geospatial.Tests;

public class GeospatialEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IGeospatialService _mockService;
    private readonly IIntelService _mockIntelService;
    private static readonly GeometryFactory Gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public GeospatialEndpointTests(WebApplicationFactory<Program> factory)
    {
        _mockService = Substitute.For<IGeospatialService>();
        _mockIntelService = Substitute.For<IIntelService>();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext-related registrations (Aspire + EF Core)
                var toRemove = services.Where(d =>
                    d.ServiceType.FullName?.Contains("GeospatialDbContext") == true ||
                    d.ServiceType == typeof(DbContextOptions<GeospatialDbContext>) ||
                    d.ImplementationType?.FullName?.Contains("GeospatialDbContext") == true).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<GeospatialDbContext>(options =>
                    options.UseInMemoryDatabase($"TestGeospatialDB-{Guid.NewGuid()}"));

                // Replace the real service with our mock
                var svcDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IGeospatialService));
                if (svcDescriptor != null) services.Remove(svcDescriptor);
                services.AddScoped<IGeospatialService>(_ => _mockService);

                // Replace intel service with mock
                var intelDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IIntelService));
                if (intelDescriptor != null) services.Remove(intelDescriptor);
                services.AddScoped<IIntelService>(_ => _mockIntelService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Info_ReturnsGeospatialInfo()
    {
        var response = await _client.GetAsync("/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Geospatial");
        body.Should().Contain("PostGIS");
    }

    [Fact]
    public async Task CreateIncidentZone_ReturnsCreated()
    {
        var zoneId = Guid.NewGuid();
        _mockService.CreateIncidentZoneAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<ZoneSeverity>())
            .Returns(new IncidentZone
            {
                Id = zoneId,
                IncidentId = Guid.NewGuid(),
                IncidentType = "ActiveShooter",
                EpicenterLocation = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                PerimeterBoundary = (Polygon)Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)).Buffer(0.005),
                InitialRadiusMeters = 500,
                CurrentRadiusMeters = 500,
                Severity = ZoneSeverity.Critical,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            IncidentId = Guid.NewGuid(),
            IncidentType = "ActiveShooter",
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 500.0,
            Severity = "Critical"
        };

        var response = await _client.PostAsJsonAsync("/api/geo/zones/incident", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ActiveShooter");
    }

    [Fact]
    public async Task RegisterTrackedEntity_ReturnsCreated()
    {
        var entityId = Guid.NewGuid();
        _mockService.RegisterTrackedEntityAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Alpha",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            EntityType = "Responder",
            ExternalEntityId = Guid.NewGuid(),
            DisplayName = "Officer Alpha",
            Longitude = -97.74,
            Latitude = 30.27
        };

        var response = await _client.PostAsJsonAsync("/api/geo/tracking/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Officer Alpha");
    }

    [Fact]
    public async Task CreateFamilyGeofence_ReturnsCreated()
    {
        var fenceId = Guid.NewGuid();
        _mockService.CreateFamilyGeofenceAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<GeofenceAlertType>())
            .Returns(new FamilyGeofence
            {
                Id = fenceId,
                FamilyGroupId = Guid.NewGuid(),
                Name = "Home Zone",
                Boundary = (Polygon)Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)).Buffer(0.002),
                Center = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                RadiusMeters = 200,
                AlertType = GeofenceAlertType.Both,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            FamilyGroupId = Guid.NewGuid(),
            Name = "Home Zone",
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 200.0,
            AlertType = "Both"
        };

        var response = await _client.PostAsJsonAsync("/api/geo/geofences/family", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Home Zone");
    }

    [Fact]
    public async Task CalculateDispatchRoute_ReturnsCreated()
    {
        var routeId = Guid.NewGuid();
        _mockService.CalculateDispatchRouteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
            .Returns(new DispatchRoute
            {
                Id = routeId,
                ResponderId = Guid.NewGuid(),
                IncidentId = Guid.NewGuid(),
                Origin = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Destination = Gf.CreatePoint(new Coordinate(-97.75, 30.28)),
                RoutePath = Gf.CreateLineString(new[] { new Coordinate(-97.74, 30.27), new Coordinate(-97.75, 30.28) }),
                DistanceMeters = 1500,
                EstimatedMinutes = 2.5,
                RouteStatus = DispatchRouteStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            ResponderId = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            OriginLon = -97.74,
            OriginLat = 30.27,
            DestLon = -97.75,
            DestLat = 30.28
        };

        var response = await _client.PostAsJsonAsync("/api/geo/routes/dispatch", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateEvacuationRoute_ReturnsCreated()
    {
        var dzId = Guid.NewGuid();
        _mockService.CreateDisasterZoneAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Coordinate[]>(),
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<ZoneSeverity>())
            .Returns(new DisasterZone
            {
                Id = dzId,
                DisasterEventId = Guid.NewGuid(),
                DisasterType = "Flood",
                AffectedArea = Gf.CreateMultiPolygon(new[]
                {
                    Gf.CreatePolygon(new[]
                    {
                        new Coordinate(-97.74, 30.26), new Coordinate(-97.75, 30.26),
                        new Coordinate(-97.75, 30.27), new Coordinate(-97.74, 30.27),
                        new Coordinate(-97.74, 30.26)
                    })
                }),
                Epicenter = Gf.CreatePoint(new Coordinate(-97.745, 30.265)),
                Severity = ZoneSeverity.High,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var routeId = Guid.NewGuid();
        _mockService.CreateEvacuationRouteAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Coordinate[]>(), Arg.Any<int>())
            .Returns(new EvacuationRoute
            {
                Id = routeId,
                DisasterZoneId = dzId,
                RouteName = "Route Alpha",
                Path = Gf.CreateLineString(new[]
                {
                    new Coordinate(-97.74, 30.26), new Coordinate(-97.73, 30.25), new Coordinate(-97.72, 30.24)
                }),
                StartPoint = Gf.CreatePoint(new Coordinate(-97.74, 30.26)),
                EndPoint = Gf.CreatePoint(new Coordinate(-97.72, 30.24)),
                DistanceMeters = 3000,
                EstimatedMinutes = 4.5,
                CapacityPersons = 500,
                RouteStatus = EvacRouteStatus.Open,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // First create a disaster zone
        var dzRequest = new
        {
            DisasterEventId = Guid.NewGuid(),
            DisasterType = "Flood",
            BoundaryPoints = new[]
            {
                new { Longitude = -97.74, Latitude = 30.26 },
                new { Longitude = -97.75, Latitude = 30.26 },
                new { Longitude = -97.75, Latitude = 30.27 },
                new { Longitude = -97.74, Latitude = 30.27 }
            },
            CenterLongitude = -97.745,
            CenterLatitude = 30.265,
            Severity = "High"
        };

        var dzResponse = await _client.PostAsJsonAsync("/api/geo/zones/disaster", dzRequest);
        dzResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var routeRequest = new
        {
            DisasterZoneId = dzId,
            Name = "Route Alpha",
            Waypoints = new[]
            {
                new { Longitude = -97.74, Latitude = 30.26 },
                new { Longitude = -97.73, Latitude = 30.25 },
                new { Longitude = -97.72, Latitude = 30.24 }
            },
            CapacityPersons = 500
        };

        var response = await _client.PostAsJsonAsync("/api/geo/routes/evacuation", routeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Route Alpha");
    }

    [Fact]
    public async Task UpdateEntityLocation_ReturnsOk()
    {
        var entityId = Guid.NewGuid();

        _mockService.RegisterTrackedEntityAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Bravo",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        _mockService.UpdateEntityLocationAsync(
            Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Bravo",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.75, 30.28)),
                LastSpeed = 15.5,
                LastHeading = 90.0,
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // Register first
        var regRequest = new
        {
            EntityType = "Responder",
            ExternalEntityId = Guid.NewGuid(),
            DisplayName = "Officer Bravo",
            Longitude = -97.74,
            Latitude = 30.27
        };

        var regResponse = await _client.PostAsJsonAsync("/api/geo/tracking/register", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update location
        var updateRequest = new
        {
            Longitude = -97.75,
            Latitude = 30.28,
            Speed = 15.5,
            Heading = 90.0
        };

        var response = await _client.PutAsJsonAsync($"/api/geo/tracking/{entityId}/location", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Officer Bravo");
    }

    [Fact]
    public async Task NearestResponders_EndpointExists()
    {
        _mockService.FindNearestRespondersAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(new List<NearbyResult>());

        var response = await _client.GetAsync("/api/geo/nearest/responders?lon=-97.7431&lat=30.2672&count=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ZoneContains_EndpointExists()
    {
        var zoneId = Guid.NewGuid();
        _mockService.IsPointInZoneAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<Guid>())
            .Returns(false);

        var response = await _client.GetAsync($"/api/geo/zones/contains?lon=-97.7431&lat=30.2672&zoneId={zoneId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Intel Caching & Inferencing Tests ───

    [Fact]
    public async Task IngestIntelEntry_ReturnsCreated()
    {
        var entryId = Guid.NewGuid();
        _mockIntelService.IngestEntryAsync(Arg.Any<IngestIntelEntryRequest>())
            .Returns(new IntelEntry
            {
                Id = entryId,
                Title = "Civil unrest reported near downtown",
                Summary = "Multiple sources confirm protests with road blockages.",
                SourceType = "News",
                SourceUrl = "https://example.com/news/civil-unrest",
                SourceName = "AP Wire",
                Location = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                RadiusMeters = 2000,
                Category = IntelCategory.CivilUnrest,
                ThreatLevel = IntelThreatLevel.Elevated,
                ConfidenceScore = 0.85,
                Tags = new Dictionary<string, string> { ["region"] = "downtown" },
                IsActive = true,
                PublishedAt = DateTimeOffset.UtcNow.AddHours(-1),
                IngestedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            Title = "Civil unrest reported near downtown",
            Summary = "Multiple sources confirm protests with road blockages.",
            SourceType = "News",
            SourceUrl = "https://example.com/news/civil-unrest",
            SourceName = "AP Wire",
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 2000.0,
            Category = "CivilUnrest",
            ThreatLevel = "Elevated",
            ConfidenceScore = 0.85,
            PublishedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var response = await _client.PostAsJsonAsync("/api/intel/ingest", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Civil unrest");
        body.Should().Contain("CivilUnrest");
    }

    [Fact]
    public async Task QueryIntelEntries_ReturnsOk()
    {
        _mockIntelService.QueryEntriesNearAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<IntelCategory?>(), Arg.Any<IntelThreatLevel?>(), Arg.Any<int>())
            .Returns(new List<IntelEntry>
            {
                new IntelEntry
                {
                    Id = Guid.NewGuid(),
                    Title = "Hazardous material spill on highway",
                    Summary = "Chemical tanker overturned — avoid area.",
                    SourceType = "FieldReport",
                    SourceUrl = "",
                    SourceName = "Field Unit 7",
                    Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                    RadiusMeters = 500,
                    Category = IntelCategory.HazardousMaterial,
                    ThreatLevel = IntelThreatLevel.High,
                    ConfidenceScore = 0.95,
                    Tags = new Dictionary<string, string>(),
                    IsActive = true,
                    PublishedAt = DateTimeOffset.UtcNow,
                    IngestedAt = DateTimeOffset.UtcNow
                }
            });

        var response = await _client.GetAsync("/api/intel/entries?lon=-97.7431&lat=30.2672&radius=5000&category=HazardousMaterial");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("HazardousMaterial");
        body.Should().Contain("Hazardous material spill");
    }

    [Fact]
    public async Task GetInferencesNear_ReturnsOk()
    {
        _mockIntelService.GetInferencesNearAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
            .Returns(new List<IntelInference>
            {
                new IntelInference
                {
                    Id = Guid.NewGuid(),
                    Location = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                    RadiusMeters = 5000,
                    Category = IntelCategory.CivilUnrest,
                    ThreatLevel = IntelThreatLevel.Elevated,
                    Summary = "CivilUnrest assessment: Elevated threat level based on 3 source(s).",
                    ConfidenceScore = 0.80,
                    SupportingEntryCount = 3,
                    IsActive = true,
                    GeneratedAt = DateTimeOffset.UtcNow
                }
            });

        var response = await _client.GetAsync("/api/intel/inferences?lon=-97.7431&lat=30.2672&radius=10000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("CivilUnrest");
        body.Should().Contain("Elevated");
    }

    [Fact]
    public async Task GenerateInference_ReturnsCreated()
    {
        var inferenceId = Guid.NewGuid();
        _mockIntelService.GenerateInferenceAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<IntelCategory>())
            .Returns(new IntelInference
            {
                Id = inferenceId,
                Location = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                RadiusMeters = 5000,
                Category = IntelCategory.Terrorism,
                ThreatLevel = IntelThreatLevel.High,
                Summary = "Terrorism assessment: High threat level based on 2 source(s) within 5000 m. Avg confidence: 90%. Top source: FBI Bulletin.",
                ConfidenceScore = 0.90,
                SupportingEntryCount = 2,
                IsActive = true,
                GeneratedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(6)
            });

        var request = new
        {
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 5000.0,
            Category = "Terrorism"
        };

        var response = await _client.PostAsJsonAsync("/api/intel/inferences/generate", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Terrorism");
        body.Should().Contain("High");
    }

    [Fact]
    public async Task QueryIntelEntries_WithNoFilters_ReturnsOk()
    {
        _mockIntelService.QueryEntriesNearAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<IntelCategory?>(), Arg.Any<IntelThreatLevel?>(), Arg.Any<int>())
            .Returns(new List<IntelEntry>());

        var response = await _client.GetAsync("/api/intel/entries?lon=-97.7431&lat=30.2672&radius=10000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Info_ReturnsGeospatialInfo()
    {
        var response = await _client.GetAsync("/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Geospatial");
        body.Should().Contain("PostGIS");
    }

    [Fact]
    public async Task CreateIncidentZone_ReturnsCreated()
    {
        var zoneId = Guid.NewGuid();
        _mockService.CreateIncidentZoneAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<ZoneSeverity>())
            .Returns(new IncidentZone
            {
                Id = zoneId,
                IncidentId = Guid.NewGuid(),
                IncidentType = "ActiveShooter",
                EpicenterLocation = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                PerimeterBoundary = (Polygon)Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)).Buffer(0.005),
                InitialRadiusMeters = 500,
                CurrentRadiusMeters = 500,
                Severity = ZoneSeverity.Critical,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            IncidentId = Guid.NewGuid(),
            IncidentType = "ActiveShooter",
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 500.0,
            Severity = "Critical"
        };

        var response = await _client.PostAsJsonAsync("/api/geo/zones/incident", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ActiveShooter");
    }

    [Fact]
    public async Task RegisterTrackedEntity_ReturnsCreated()
    {
        var entityId = Guid.NewGuid();
        _mockService.RegisterTrackedEntityAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Alpha",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            EntityType = "Responder",
            ExternalEntityId = Guid.NewGuid(),
            DisplayName = "Officer Alpha",
            Longitude = -97.74,
            Latitude = 30.27
        };

        var response = await _client.PostAsJsonAsync("/api/geo/tracking/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Officer Alpha");
    }

    [Fact]
    public async Task CreateFamilyGeofence_ReturnsCreated()
    {
        var fenceId = Guid.NewGuid();
        _mockService.CreateFamilyGeofenceAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<GeofenceAlertType>())
            .Returns(new FamilyGeofence
            {
                Id = fenceId,
                FamilyGroupId = Guid.NewGuid(),
                Name = "Home Zone",
                Boundary = (Polygon)Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)).Buffer(0.002),
                Center = Gf.CreatePoint(new Coordinate(-97.7431, 30.2672)),
                RadiusMeters = 200,
                AlertType = GeofenceAlertType.Both,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            FamilyGroupId = Guid.NewGuid(),
            Name = "Home Zone",
            Longitude = -97.7431,
            Latitude = 30.2672,
            RadiusMeters = 200.0,
            AlertType = "Both"
        };

        var response = await _client.PostAsJsonAsync("/api/geo/geofences/family", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Home Zone");
    }

    [Fact]
    public async Task CalculateDispatchRoute_ReturnsCreated()
    {
        var routeId = Guid.NewGuid();
        _mockService.CalculateDispatchRouteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
            .Returns(new DispatchRoute
            {
                Id = routeId,
                ResponderId = Guid.NewGuid(),
                IncidentId = Guid.NewGuid(),
                Origin = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Destination = Gf.CreatePoint(new Coordinate(-97.75, 30.28)),
                RoutePath = Gf.CreateLineString(new[] { new Coordinate(-97.74, 30.27), new Coordinate(-97.75, 30.28) }),
                DistanceMeters = 1500,
                EstimatedMinutes = 2.5,
                RouteStatus = DispatchRouteStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var request = new
        {
            ResponderId = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            OriginLon = -97.74,
            OriginLat = 30.27,
            DestLon = -97.75,
            DestLat = 30.28
        };

        var response = await _client.PostAsJsonAsync("/api/geo/routes/dispatch", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateEvacuationRoute_ReturnsCreated()
    {
        var dzId = Guid.NewGuid();
        _mockService.CreateDisasterZoneAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Coordinate[]>(),
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<ZoneSeverity>())
            .Returns(new DisasterZone
            {
                Id = dzId,
                DisasterEventId = Guid.NewGuid(),
                DisasterType = "Flood",
                AffectedArea = Gf.CreateMultiPolygon(new[]
                {
                    Gf.CreatePolygon(new[]
                    {
                        new Coordinate(-97.74, 30.26), new Coordinate(-97.75, 30.26),
                        new Coordinate(-97.75, 30.27), new Coordinate(-97.74, 30.27),
                        new Coordinate(-97.74, 30.26)
                    })
                }),
                Epicenter = Gf.CreatePoint(new Coordinate(-97.745, 30.265)),
                Severity = ZoneSeverity.High,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var routeId = Guid.NewGuid();
        _mockService.CreateEvacuationRouteAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Coordinate[]>(), Arg.Any<int>())
            .Returns(new EvacuationRoute
            {
                Id = routeId,
                DisasterZoneId = dzId,
                RouteName = "Route Alpha",
                Path = Gf.CreateLineString(new[]
                {
                    new Coordinate(-97.74, 30.26), new Coordinate(-97.73, 30.25), new Coordinate(-97.72, 30.24)
                }),
                StartPoint = Gf.CreatePoint(new Coordinate(-97.74, 30.26)),
                EndPoint = Gf.CreatePoint(new Coordinate(-97.72, 30.24)),
                DistanceMeters = 3000,
                EstimatedMinutes = 4.5,
                CapacityPersons = 500,
                RouteStatus = EvacRouteStatus.Open,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // First create a disaster zone
        var dzRequest = new
        {
            DisasterEventId = Guid.NewGuid(),
            DisasterType = "Flood",
            BoundaryPoints = new[]
            {
                new { Longitude = -97.74, Latitude = 30.26 },
                new { Longitude = -97.75, Latitude = 30.26 },
                new { Longitude = -97.75, Latitude = 30.27 },
                new { Longitude = -97.74, Latitude = 30.27 }
            },
            CenterLongitude = -97.745,
            CenterLatitude = 30.265,
            Severity = "High"
        };

        var dzResponse = await _client.PostAsJsonAsync("/api/geo/zones/disaster", dzRequest);
        dzResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var routeRequest = new
        {
            DisasterZoneId = dzId,
            Name = "Route Alpha",
            Waypoints = new[]
            {
                new { Longitude = -97.74, Latitude = 30.26 },
                new { Longitude = -97.73, Latitude = 30.25 },
                new { Longitude = -97.72, Latitude = 30.24 }
            },
            CapacityPersons = 500
        };

        var response = await _client.PostAsJsonAsync("/api/geo/routes/evacuation", routeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Route Alpha");
    }

    [Fact]
    public async Task UpdateEntityLocation_ReturnsOk()
    {
        var entityId = Guid.NewGuid();

        _mockService.RegisterTrackedEntityAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Bravo",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        _mockService.UpdateEntityLocationAsync(
            Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<double>())
            .Returns(new TrackedEntity
            {
                Id = entityId,
                EntityType = "Responder",
                ExternalEntityId = Guid.NewGuid(),
                DisplayName = "Officer Bravo",
                LastKnownLocation = Gf.CreatePoint(new Coordinate(-97.75, 30.28)),
                LastSpeed = 15.5,
                LastHeading = 90.0,
                Status = TrackingStatus.Active,
                LastUpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // Register first
        var regRequest = new
        {
            EntityType = "Responder",
            ExternalEntityId = Guid.NewGuid(),
            DisplayName = "Officer Bravo",
            Longitude = -97.74,
            Latitude = 30.27
        };

        var regResponse = await _client.PostAsJsonAsync("/api/geo/tracking/register", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update location
        var updateRequest = new
        {
            Longitude = -97.75,
            Latitude = 30.28,
            Speed = 15.5,
            Heading = 90.0
        };

        var response = await _client.PutAsJsonAsync($"/api/geo/tracking/{entityId}/location", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Officer Bravo");
    }

    [Fact]
    public async Task NearestResponders_EndpointExists()
    {
        _mockService.FindNearestRespondersAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(new List<NearbyResult>());

        var response = await _client.GetAsync("/api/geo/nearest/responders?lon=-97.7431&lat=30.2672&count=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ZoneContains_EndpointExists()
    {
        var zoneId = Guid.NewGuid();
        _mockService.IsPointInZoneAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<Guid>())
            .Returns(false);

        var response = await _client.GetAsync($"/api/geo/zones/contains?lon=-97.7431&lat=30.2672&zoneId={zoneId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
