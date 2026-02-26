using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace TheWatch.Geospatial.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "DisasterZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AffectedArea = table.Column<MultiPolygon>(type: "geometry (MultiPolygon, 4326)", nullable: false),
                    Epicenter = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedAffectedPopulation = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Destination = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    RoutePath = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedMinutes = table.Column<double>(type: "double precision", nullable: false),
                    RouteStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvacuationRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Path = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false),
                    StartPoint = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    EndPoint = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedMinutes = table.Column<double>(type: "double precision", nullable: false),
                    CapacityPersons = table.Column<int>(type: "integer", nullable: false),
                    RouteStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvacuationRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilyGeofences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Boundary = table.Column<Polygon>(type: "geometry (Polygon, 4326)", nullable: false),
                    Center = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    RadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyGeofences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMemberLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    IsInsideGeofence = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveGeofenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMemberLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeofenceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyGeofenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofenceEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoFences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Boundary = table.Column<Polygon>(type: "geometry (Polygon, 4326)", nullable: false),
                    RadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    Center = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    FenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerEntityType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoFences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Altitude = table.Column<double>(type: "double precision", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    LocationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Boundary = table.Column<Polygon>(type: "geometry (Polygon, 4326)", nullable: false),
                    Centroid = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    ZoneType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ParentZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EpicenterLocation = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    PerimeterBoundary = table.Column<Polygon>(type: "geometry (Polygon, 4326)", nullable: false),
                    InitialRadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    CurrentRadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackedEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: false),
                    Heading = table.Column<double>(type: "double precision", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointOfInterests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEmergencyFacility = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointOfInterests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponderPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: false),
                    Heading = table.Column<double>(type: "double precision", nullable: false),
                    DispatchStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DistanceToIncidentMeters = table.Column<double>(type: "double precision", nullable: true),
                    EtaMinutes = table.Column<double>(type: "double precision", nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponderPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShelterLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisasterZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentOccupancy = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HasMedical = table.Column<bool>(type: "boolean", nullable: false),
                    HasPower = table.Column<bool>(type: "boolean", nullable: false),
                    HasWater = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelterLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpatialSearchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    Label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FoundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpatialSearchResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackedEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastKnownLocation = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    LastSpeed = table.Column<double>(type: "double precision", nullable: false),
                    LastHeading = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackedEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<LineString>(type: "geometry (LineString, 4326)", nullable: false),
                    TotalDistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    SessionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_AffectedArea",
                table: "DisasterZones",
                column: "AffectedArea")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_CreatedAt",
                table: "DisasterZones",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_DisasterEventId",
                table: "DisasterZones",
                column: "DisasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_Epicenter",
                table: "DisasterZones",
                column: "Epicenter")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_ResolvedAt",
                table: "DisasterZones",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterZones_Severity",
                table: "DisasterZones",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_CreatedAt",
                table: "DispatchRoutes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_Destination",
                table: "DispatchRoutes",
                column: "Destination")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_IncidentId",
                table: "DispatchRoutes",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_Origin",
                table: "DispatchRoutes",
                column: "Origin")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_ResponderId",
                table: "DispatchRoutes",
                column: "ResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_RoutePath",
                table: "DispatchRoutes",
                column: "RoutePath")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRoutes_RouteStatus",
                table: "DispatchRoutes",
                column: "RouteStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_CreatedAt",
                table: "EvacuationRoutes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_DisasterZoneId",
                table: "EvacuationRoutes",
                column: "DisasterZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_EndPoint",
                table: "EvacuationRoutes",
                column: "EndPoint")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_Path",
                table: "EvacuationRoutes",
                column: "Path")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_RouteStatus",
                table: "EvacuationRoutes",
                column: "RouteStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_StartPoint",
                table: "EvacuationRoutes",
                column: "StartPoint")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGeofences_AlertType",
                table: "FamilyGeofences",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGeofences_Boundary",
                table: "FamilyGeofences",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGeofences_Center",
                table: "FamilyGeofences",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGeofences_CreatedAt",
                table: "FamilyGeofences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyGeofences_FamilyGroupId",
                table: "FamilyGeofences",
                column: "FamilyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberLocations_ActiveGeofenceId",
                table: "FamilyMemberLocations",
                column: "ActiveGeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberLocations_FamilyGroupId",
                table: "FamilyMemberLocations",
                column: "FamilyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberLocations_Location",
                table: "FamilyMemberLocations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberLocations_MemberId",
                table: "FamilyMemberLocations",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberLocations_RecordedAt",
                table: "FamilyMemberLocations",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_EventType",
                table: "GeofenceEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_FamilyGeofenceId",
                table: "GeofenceEvents",
                column: "FamilyGeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_Location",
                table: "GeofenceEvents",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_MemberId",
                table: "GeofenceEvents",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_OccurredAt",
                table: "GeofenceEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_Boundary",
                table: "GeoFences",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_Center",
                table: "GeoFences",
                column: "Center")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_CreatedAt",
                table: "GeoFences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_FenceType",
                table: "GeoFences",
                column: "FenceType");

            migrationBuilder.CreateIndex(
                name: "IX_GeoFences_OwnerEntityId",
                table: "GeoFences",
                column: "OwnerEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GeoLocations_CreatedAt",
                table: "GeoLocations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoLocations_Location",
                table: "GeoLocations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeoLocations_LocationType",
                table: "GeoLocations",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_GeoLocations_RecordedAt",
                table: "GeoLocations",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_Boundary",
                table: "GeoZones",
                column: "Boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_Centroid",
                table: "GeoZones",
                column: "Centroid")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_CreatedAt",
                table: "GeoZones",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_ExpiresAt",
                table: "GeoZones",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_ParentZoneId",
                table: "GeoZones",
                column: "ParentZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_Severity",
                table: "GeoZones",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_GeoZones_ZoneType",
                table: "GeoZones",
                column: "ZoneType");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_CreatedAt",
                table: "IncidentZones",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_EpicenterLocation",
                table: "IncidentZones",
                column: "EpicenterLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_IncidentId",
                table: "IncidentZones",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_PerimeterBoundary",
                table: "IncidentZones",
                column: "PerimeterBoundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_ResolvedAt",
                table: "IncidentZones",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentZones_Severity",
                table: "IncidentZones",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_LocationHistories_Location",
                table: "LocationHistories",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_LocationHistories_RecordedAt",
                table: "LocationHistories",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationHistories_TrackedEntityId",
                table: "LocationHistories",
                column: "TrackedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PointOfInterests_CreatedAt",
                table: "PointOfInterests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PointOfInterests_Location",
                table: "PointOfInterests",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ResponderPositions_DispatchStatus",
                table: "ResponderPositions",
                column: "DispatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResponderPositions_IncidentId",
                table: "ResponderPositions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponderPositions_Location",
                table: "ResponderPositions",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ResponderPositions_ReportedAt",
                table: "ResponderPositions",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResponderPositions_ResponderId",
                table: "ResponderPositions",
                column: "ResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterLocations_CreatedAt",
                table: "ShelterLocations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterLocations_DisasterZoneId",
                table: "ShelterLocations",
                column: "DisasterZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterLocations_Location",
                table: "ShelterLocations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ShelterLocations_Status",
                table: "ShelterLocations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SpatialSearchResults_EntityId",
                table: "SpatialSearchResults",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SpatialSearchResults_FoundAt",
                table: "SpatialSearchResults",
                column: "FoundAt");

            migrationBuilder.CreateIndex(
                name: "IX_SpatialSearchResults_Location",
                table: "SpatialSearchResults",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEntities_CreatedAt",
                table: "TrackedEntities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEntities_ExternalEntityId",
                table: "TrackedEntities",
                column: "ExternalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEntities_LastKnownLocation",
                table: "TrackedEntities",
                column: "LastKnownLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEntities_LastUpdatedAt",
                table: "TrackedEntities",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedEntities_Status",
                table: "TrackedEntities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_EndedAt",
                table: "TrackingSessions",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_Path",
                table: "TrackingSessions",
                column: "Path")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_SessionStatus",
                table: "TrackingSessions",
                column: "SessionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_StartedAt",
                table: "TrackingSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_TrackedEntityId",
                table: "TrackingSessions",
                column: "TrackedEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisasterZones");

            migrationBuilder.DropTable(
                name: "DispatchRoutes");

            migrationBuilder.DropTable(
                name: "EvacuationRoutes");

            migrationBuilder.DropTable(
                name: "FamilyGeofences");

            migrationBuilder.DropTable(
                name: "FamilyMemberLocations");

            migrationBuilder.DropTable(
                name: "GeofenceEvents");

            migrationBuilder.DropTable(
                name: "GeoFences");

            migrationBuilder.DropTable(
                name: "GeoLocations");

            migrationBuilder.DropTable(
                name: "GeoZones");

            migrationBuilder.DropTable(
                name: "IncidentZones");

            migrationBuilder.DropTable(
                name: "LocationHistories");

            migrationBuilder.DropTable(
                name: "PointOfInterests");

            migrationBuilder.DropTable(
                name: "ResponderPositions");

            migrationBuilder.DropTable(
                name: "ShelterLocations");

            migrationBuilder.DropTable(
                name: "SpatialSearchResults");

            migrationBuilder.DropTable(
                name: "TrackedEntities");

            migrationBuilder.DropTable(
                name: "TrackingSessions");
        }
    }
}
