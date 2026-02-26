-- ============================================================================
-- TheWatch — SQL Server Graph Database Schema
-- Social graph + incident correlation using SQL Server graph tables (node/edge)
-- Requires: SQL Server 2017+ with graph database support
-- ============================================================================

USE WatchCoreGatewayDB;
GO

-- ============================================================================
-- SCHEMA
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'graph')
    EXEC('CREATE SCHEMA graph');
GO

-- ============================================================================
-- NODE TABLES — Entities in the social/operational graph
-- ============================================================================

-- Person: users, family members, responders, doctors, community watch members
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Person' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Person (
    PersonId        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NOT NULL,               -- Maps to P5 UserId / P7 MemberId / P6 ResponderId
    PersonType      NVARCHAR(50)     NOT NULL,               -- User, FamilyMember, Responder, Doctor, CommunityWatch
    DisplayName     NVARCHAR(200)    NOT NULL,
    Email           NVARCHAR(256)    NULL,
    ServiceOrigin   NVARCHAR(50)     NOT NULL,               -- P1, P3, P5, P6, P7, P9
    IsActive        BIT              NOT NULL DEFAULT 1,
    TrustScore      FLOAT            NOT NULL DEFAULT 0.5,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2        NULL,

    CONSTRAINT PK_graph_Person PRIMARY KEY (PersonId),
    CONSTRAINT UQ_graph_Person_External UNIQUE (ExternalId, PersonType),
    INDEX IX_graph_Person_Type (PersonType),
    INDEX IX_graph_Person_Service (ServiceOrigin)
) AS NODE;
GO

-- Incident: emergency events, alerts, reports across P2, P5, P8
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Incident' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Incident (
    IncidentId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NOT NULL,               -- Maps to P2 IncidentId / P8 DisasterEventId
    IncidentType    NVARCHAR(100)    NOT NULL,               -- ActiveShooter, MedicalEmergency, Wildfire, Flood, etc.
    Severity        INT              NOT NULL DEFAULT 3,     -- 1-5
    Title           NVARCHAR(500)    NOT NULL,
    Latitude        FLOAT            NULL,
    Longitude       FLOAT            NULL,
    Status          NVARCHAR(50)     NOT NULL DEFAULT 'Active',
    ServiceOrigin   NVARCHAR(50)     NOT NULL,               -- P2, P8
    OccurredAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ResolvedAt      DATETIME2        NULL,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_graph_Incident PRIMARY KEY (IncidentId),
    CONSTRAINT UQ_graph_Incident_External UNIQUE (ExternalId),
    INDEX IX_graph_Incident_Type (IncidentType),
    INDEX IX_graph_Incident_Severity (Severity),
    INDEX IX_graph_Incident_Status (Status),
    INDEX IX_graph_Incident_OccurredAt (OccurredAt DESC)
) AS NODE;
GO

-- Location: neighborhoods, zones, geofences, shelters
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Location' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Location (
    LocationId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NULL,
    LocationType    NVARCHAR(50)     NOT NULL,               -- Neighborhood, Zone, Shelter, Hospital, Station
    Name            NVARCHAR(300)    NOT NULL,
    Latitude        FLOAT            NOT NULL,
    Longitude       FLOAT            NOT NULL,
    RadiusKm        FLOAT            NULL,
    GeoHash         NVARCHAR(20)     NULL,
    City            NVARCHAR(100)    NULL,
    State           NVARCHAR(100)    NULL,
    Country         NVARCHAR(5)      NOT NULL DEFAULT 'US',
    ServiceOrigin   NVARCHAR(50)     NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_graph_Location PRIMARY KEY (LocationId),
    INDEX IX_graph_Location_Type (LocationType),
    INDEX IX_graph_Location_GeoHash (GeoHash)
) AS NODE;
GO

-- Organization: agencies, hospitals, fire departments, community groups
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Organization' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Organization (
    OrganizationId  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NULL,
    OrgType         NVARCHAR(50)     NOT NULL,               -- PoliceDept, FireDept, Hospital, CommunityGroup, EMSAgency
    Name            NVARCHAR(300)    NOT NULL,
    JurisdictionId  UNIQUEIDENTIFIER NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_graph_Organization PRIMARY KEY (OrganizationId),
    INDEX IX_graph_Organization_Type (OrgType)
) AS NODE;
GO

-- Device: wearables, mesh nodes, IoT sensors
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Device' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Device (
    DeviceId        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NOT NULL,               -- Maps to P4 WearableDeviceId / P3 MeshNodeId
    DeviceType      NVARCHAR(50)     NOT NULL,               -- Wearable, MeshNode, IoTSensor, Phone
    Model           NVARCHAR(200)    NULL,
    ServiceOrigin   NVARCHAR(50)     NOT NULL,               -- P3, P4
    IsActive        BIT              NOT NULL DEFAULT 1,
    LastSeenAt      DATETIME2        NULL,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_graph_Device PRIMARY KEY (DeviceId),
    CONSTRAINT UQ_graph_Device_External UNIQUE (ExternalId),
    INDEX IX_graph_Device_Type (DeviceType)
) AS NODE;
GO

-- Resource: supplies, equipment, vehicles (P8 disaster relief)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Resource' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.Resource (
    ResourceId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExternalId      UNIQUEIDENTIFIER NULL,
    ResourceType    NVARCHAR(100)    NOT NULL,               -- MedicalSupply, Vehicle, Equipment, Shelter
    Name            NVARCHAR(300)    NOT NULL,
    Quantity        INT              NOT NULL DEFAULT 1,
    Status          NVARCHAR(50)     NOT NULL DEFAULT 'Available',
    ServiceOrigin   NVARCHAR(50)     NOT NULL DEFAULT 'P8',
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_graph_Resource PRIMARY KEY (ResourceId),
    INDEX IX_graph_Resource_Type (ResourceType)
) AS NODE;
GO

-- ============================================================================
-- EDGE TABLES — Relationships between entities
-- ============================================================================

-- FamilyOf: person ←→ person (P7 family groups)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FamilyOf' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.FamilyOf (
    FamilyGroupId   UNIQUEIDENTIFIER NOT NULL,
    Relationship    NVARCHAR(50)     NOT NULL,               -- Parent, Child, Spouse, Sibling, Guardian
    IsPrimary       BIT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_graph_FamilyOf_Group (FamilyGroupId)
) AS EDGE;
GO

-- RespondedTo: person → incident (P6 dispatch records)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RespondedTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.RespondedTo (
    DispatchId      UNIQUEIDENTIFIER NULL,
    ResponseTimeMin FLOAT            NULL,
    DistanceKm      FLOAT            NULL,
    Role            NVARCHAR(50)     NULL,                   -- Primary, Backup, Support
    Outcome         NVARCHAR(50)     NULL,                   -- Resolved, Escalated, NoAction
    ArrivedAt       DATETIME2        NULL,
    CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_graph_RespondedTo_Dispatch (DispatchId)
) AS EDGE;
GO

-- ReportedBy: incident ← person (who reported the incident)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportedBy' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.ReportedBy (
    ReportMethod    NVARCHAR(50)     NULL,                   -- Voice, App, Wearable, MeshTap, Automated
    ReportedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_graph_ReportedBy_Method (ReportMethod)
) AS EDGE;
GO

-- NearTo: location ←→ location (geographic proximity / adjacency)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NearTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.NearTo (
    DistanceKm      FLOAT            NOT NULL,
    IsAdjacent      BIT              NOT NULL DEFAULT 0,
    ComputedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
) AS EDGE;
GO

-- CorrelatedWith: incident ←→ incident (temporal/spatial correlation)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CorrelatedWith' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.CorrelatedWith (
    CorrelationType NVARCHAR(50)     NOT NULL,               -- Spatial, Temporal, Causal, TypeMatch
    ConfidenceScore FLOAT            NOT NULL DEFAULT 0.5,   -- 0.0–1.0
    DistanceKm      FLOAT            NULL,
    TimeDeltaMin    FLOAT            NULL,
    Notes           NVARCHAR(500)    NULL,
    ComputedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_graph_Correlated_Type (CorrelationType),
    INDEX IX_graph_Correlated_Confidence (ConfidenceScore)
) AS EDGE;
GO

-- BelongsTo: person → organization
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BelongsTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.BelongsTo (
    Role            NVARCHAR(100)    NULL,                   -- Officer, Paramedic, Volunteer, Doctor
    BadgeNumber     NVARCHAR(50)     NULL,
    JoinedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    LeftAt          DATETIME2        NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,

    INDEX IX_graph_BelongsTo_Active (IsActive)
) AS EDGE;
GO

-- LocatedIn: person/incident/device/resource → location
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocatedIn' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.LocatedIn (
    Latitude        FLOAT            NULL,
    Longitude       FLOAT            NULL,
    Precision       NVARCHAR(20)     NULL,                   -- GPS, CellTower, Wifi, Manual
    RecordedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
) AS EDGE;
GO

-- ConnectedTo: device ←→ device (mesh network connectivity)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConnectedTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.ConnectedTo (
    ConnectionType  NVARCHAR(50)     NOT NULL,               -- Bluetooth, Wifi, LoRa, BLE
    SignalStrengthDbm FLOAT          NULL,
    LatencyMs       FLOAT            NULL,
    BandwidthKbps   FLOAT            NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,
    EstablishedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    DisconnectedAt  DATETIME2        NULL,

    INDEX IX_graph_Connected_Active (IsActive)
) AS EDGE;
GO

-- AssignedTo: device → person (device ownership)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssignedTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.AssignedTo (
    AssignedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UnassignedAt    DATETIME2        NULL,
    IsActive        BIT              NOT NULL DEFAULT 1
) AS EDGE;
GO

-- MonitoredBy: person → person (doctor/patient, guardian/child relationships)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MonitoredBy' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.MonitoredBy (
    MonitorType     NVARCHAR(50)     NOT NULL,               -- Medical, Guardian, CommunityWatch
    ConsentGranted  BIT              NOT NULL DEFAULT 0,
    StartedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    EndedAt         DATETIME2        NULL,
    IsActive        BIT              NOT NULL DEFAULT 1,

    INDEX IX_graph_Monitored_Type (MonitorType)
) AS EDGE;
GO

-- WitnessedBy: incident ← person (eyewitness / bystander)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WitnessedBy' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.WitnessedBy (
    StatementGiven  BIT              NOT NULL DEFAULT 0,
    DistanceMeters  FLOAT            NULL,
    RecordedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
) AS EDGE;
GO

-- DeployedTo: resource → location (resource deployment for disasters)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DeployedTo' AND schema_id = SCHEMA_ID('graph'))
CREATE TABLE graph.DeployedTo (
    Quantity        INT              NOT NULL DEFAULT 1,
    DeployedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    RetrievedAt     DATETIME2        NULL,
    RequestedBy     UNIQUEIDENTIFIER NULL
) AS EDGE;
GO

-- ============================================================================
-- GRAPH QUERY VIEWS — Pre-built traversals for common patterns
-- ============================================================================

-- View: Full incident response chain (who reported → incident → who responded)
GO
CREATE OR ALTER VIEW graph.vw_IncidentResponseChain AS
    SELECT
        reporter.PersonId       AS ReporterId,
        reporter.DisplayName    AS ReporterName,
        reporter.PersonType     AS ReporterType,
        rb.ReportMethod,
        i.IncidentId,
        i.Title                 AS IncidentTitle,
        i.IncidentType,
        i.Severity,
        i.Status                AS IncidentStatus,
        i.OccurredAt,
        responder.PersonId      AS ResponderId,
        responder.DisplayName   AS ResponderName,
        responder.PersonType    AS ResponderType,
        rt.ResponseTimeMin,
        rt.DistanceKm,
        rt.Outcome
    FROM
        graph.Person AS reporter,
        graph.ReportedBy AS rb,
        graph.Incident AS i,
        graph.RespondedTo AS rt,
        graph.Person AS responder
    WHERE MATCH(i-(rb)->reporter)
      AND MATCH(responder-(rt)->i);
GO

-- View: Family network with all relationships
CREATE OR ALTER VIEW graph.vw_FamilyNetwork AS
    SELECT
        p1.PersonId             AS Person1Id,
        p1.DisplayName          AS Person1Name,
        p1.PersonType           AS Person1Type,
        fo.Relationship,
        fo.FamilyGroupId,
        p2.PersonId             AS Person2Id,
        p2.DisplayName          AS Person2Name,
        p2.PersonType           AS Person2Type
    FROM
        graph.Person AS p1,
        graph.FamilyOf AS fo,
        graph.Person AS p2
    WHERE MATCH(p1-(fo)->p2);
GO

-- View: Incident correlation clusters
CREATE OR ALTER VIEW graph.vw_IncidentCorrelations AS
    SELECT
        i1.IncidentId           AS Incident1Id,
        i1.Title                AS Incident1Title,
        i1.IncidentType         AS Incident1Type,
        i1.Severity             AS Incident1Severity,
        i1.OccurredAt           AS Incident1Time,
        cw.CorrelationType,
        cw.ConfidenceScore,
        cw.DistanceKm,
        cw.TimeDeltaMin,
        i2.IncidentId           AS Incident2Id,
        i2.Title                AS Incident2Title,
        i2.IncidentType         AS Incident2Type,
        i2.Severity             AS Incident2Severity,
        i2.OccurredAt           AS Incident2Time
    FROM
        graph.Incident AS i1,
        graph.CorrelatedWith AS cw,
        graph.Incident AS i2
    WHERE MATCH(i1-(cw)->i2);
GO

-- View: Mesh network topology
CREATE OR ALTER VIEW graph.vw_MeshTopology AS
    SELECT
        d1.DeviceId             AS Device1Id,
        d1.DeviceType           AS Device1Type,
        d1.Model                AS Device1Model,
        ct.ConnectionType,
        ct.SignalStrengthDbm,
        ct.LatencyMs,
        ct.BandwidthKbps,
        ct.IsActive,
        d2.DeviceId             AS Device2Id,
        d2.DeviceType           AS Device2Type,
        d2.Model                AS Device2Model
    FROM
        graph.Device AS d1,
        graph.ConnectedTo AS ct,
        graph.Device AS d2
    WHERE MATCH(d1-(ct)->d2);
GO

-- ============================================================================
-- STORED PROCEDURES — Graph operations
-- ============================================================================

-- Procedure: Find all people within N hops of a given person (social network traversal)
CREATE OR ALTER PROCEDURE graph.usp_FindPeopleWithinHops
    @PersonId UNIQUEIDENTIFIER,
    @MaxHops  INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PersonNetwork AS (
        -- Direct connections (1 hop via FamilyOf)
        SELECT
            p2.PersonId,
            p2.DisplayName,
            p2.PersonType,
            fo.Relationship,
            1 AS Hops
        FROM graph.Person AS p1,
             graph.FamilyOf AS fo,
             graph.Person AS p2
        WHERE MATCH(p1-(fo)->p2)
          AND p1.PersonId = @PersonId

        UNION

        -- Direct connections via MonitoredBy
        SELECT
            p2.PersonId,
            p2.DisplayName,
            p2.PersonType,
            mb.MonitorType AS Relationship,
            1 AS Hops
        FROM graph.Person AS p1,
             graph.MonitoredBy AS mb,
             graph.Person AS p2
        WHERE MATCH(p1-(mb)->p2)
          AND p1.PersonId = @PersonId
    )
    SELECT DISTINCT PersonId, DisplayName, PersonType, Relationship, Hops
    FROM PersonNetwork
    WHERE Hops <= @MaxHops
    ORDER BY Hops, DisplayName;
END;
GO

-- Procedure: Correlate incidents within a spatial/temporal window
CREATE OR ALTER PROCEDURE graph.usp_CorrelateIncidents
    @IncidentId         UNIQUEIDENTIFIER,
    @SpatialRadiusKm    FLOAT = 5.0,
    @TemporalWindowMin  INT   = 60,
    @MinConfidence      FLOAT = 0.3
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @lat FLOAT, @lon FLOAT, @occurredAt DATETIME2;

    SELECT @lat = Latitude, @lon = Longitude, @occurredAt = OccurredAt
    FROM graph.Incident
    WHERE IncidentId = @IncidentId;

    IF @lat IS NULL OR @lon IS NULL RETURN;

    -- Find spatially and temporally nearby incidents
    ;WITH NearbyIncidents AS (
        SELECT
            i.IncidentId,
            i.IncidentType,
            i.Severity,
            i.Title,
            i.OccurredAt,
            -- Simplified Haversine approximation
            SQRT(POWER(69.1 * (i.Latitude - @lat), 2) +
                 POWER(69.1 * (i.Longitude - @lon) * COS(RADIANS(@lat)), 2)) * 1.60934 AS DistanceKm,
            ABS(DATEDIFF(MINUTE, @occurredAt, i.OccurredAt)) AS TimeDeltaMin
        FROM graph.Incident AS i
        WHERE i.IncidentId <> @IncidentId
          AND i.Latitude IS NOT NULL
          AND i.Longitude IS NOT NULL
    )
    SELECT
        @IncidentId AS SourceIncidentId,
        ni.IncidentId AS CorrelatedIncidentId,
        ni.IncidentType,
        ni.Severity,
        ni.Title,
        ni.DistanceKm,
        ni.TimeDeltaMin,
        -- Confidence scoring: closer in space+time = higher confidence
        CASE
            WHEN ni.DistanceKm <= 1.0 AND ni.TimeDeltaMin <= 15 THEN 0.95
            WHEN ni.DistanceKm <= 2.0 AND ni.TimeDeltaMin <= 30 THEN 0.80
            WHEN ni.DistanceKm <= @SpatialRadiusKm AND ni.TimeDeltaMin <= @TemporalWindowMin THEN 0.50
            ELSE 0.30
        END AS ConfidenceScore,
        CASE
            WHEN ni.TimeDeltaMin <= 15 AND ni.DistanceKm <= 1.0 THEN 'Spatial+Temporal'
            WHEN ni.TimeDeltaMin <= @TemporalWindowMin THEN 'Temporal'
            WHEN ni.DistanceKm <= @SpatialRadiusKm THEN 'Spatial'
            ELSE 'Weak'
        END AS CorrelationType
    FROM NearbyIncidents ni
    WHERE ni.DistanceKm <= @SpatialRadiusKm
       OR ni.TimeDeltaMin <= @TemporalWindowMin
    ORDER BY ConfidenceScore DESC, ni.DistanceKm;
END;
GO

-- Procedure: Get responder performance summary from graph
CREATE OR ALTER PROCEDURE graph.usp_ResponderPerformance
    @ResponderId UNIQUEIDENTIFIER = NULL,
    @FromDate    DATETIME2 = NULL,
    @ToDate      DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @FromDate = ISNULL(@FromDate, DATEADD(MONTH, -3, SYSUTCDATETIME()));
    SET @ToDate   = ISNULL(@ToDate, SYSUTCDATETIME());

    SELECT
        p.PersonId              AS ResponderId,
        p.DisplayName           AS ResponderName,
        p.PersonType,
        COUNT(*)                AS TotalResponses,
        AVG(rt.ResponseTimeMin) AS AvgResponseTimeMin,
        MIN(rt.ResponseTimeMin) AS MinResponseTimeMin,
        MAX(rt.ResponseTimeMin) AS MaxResponseTimeMin,
        AVG(rt.DistanceKm)     AS AvgDistanceKm,
        SUM(CASE WHEN rt.Outcome = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,
        SUM(CASE WHEN rt.Outcome = 'Escalated' THEN 1 ELSE 0 END) AS EscalatedCount
    FROM graph.Person AS p,
         graph.RespondedTo AS rt,
         graph.Incident AS i
    WHERE MATCH(p-(rt)->i)
      AND i.OccurredAt BETWEEN @FromDate AND @ToDate
      AND (@ResponderId IS NULL OR p.PersonId = @ResponderId)
    GROUP BY p.PersonId, p.DisplayName, p.PersonType
    ORDER BY TotalResponses DESC;
END;
GO

-- ============================================================================
-- INDEXES for graph edge lookup optimization
-- ============================================================================

-- Add graph-specific computed columns for common queries
-- (SQL Server graph uses $node_id and $edge_id internally)

PRINT 'TheWatch Graph DB schema created successfully.';
PRINT 'Node tables: Person, Incident, Location, Organization, Device, Resource';
PRINT 'Edge tables: FamilyOf, RespondedTo, ReportedBy, NearTo, CorrelatedWith,';
PRINT '             BelongsTo, LocatedIn, ConnectedTo, AssignedTo, MonitoredBy,';
PRINT '             WitnessedBy, DeployedTo';
PRINT 'Views: vw_IncidentResponseChain, vw_FamilyNetwork, vw_IncidentCorrelations, vw_MeshTopology';
PRINT 'Procedures: usp_FindPeopleWithinHops, usp_CorrelateIncidents, usp_ResponderPerformance';
GO
