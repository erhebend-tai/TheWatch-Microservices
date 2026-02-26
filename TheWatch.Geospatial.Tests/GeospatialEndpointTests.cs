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
    private static readonly GeometryFactory Gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public GeospatialEndpointTests(WebApplicationFactory<Program> factory)
    {
        _mockService = Substitute.For<IGeospatialService>();

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
}
