-- ============================================================================
-- TheWatch Geospatial Schema — PostGIS
-- 28 tables, 5 schemas, 12 spatial functions
-- Deployed via EF Core migrations; this file is the reference DDL.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS postgis;

-- ─── Schema: geo_core ─────────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_core;

CREATE TABLE geo_core.geo_locations (
    id UUID PRIMARY KEY,
    label VARCHAR(500),
    description VARCHAR(4000),
    location GEOMETRY(Point, 4326) NOT NULL,
    altitude DOUBLE PRECISION DEFAULT 0,
    accuracy DOUBLE PRECISION DEFAULT 0,
    location_type VARCHAR(50) NOT NULL,
    source_id VARCHAR(500),
    source_type VARCHAR(500),
    recorded_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geo_core.geo_zones (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(4000),
    boundary GEOMETRY(Polygon, 4326) NOT NULL,
    centroid GEOMETRY(Point, 4326) NOT NULL,
    zone_type VARCHAR(50) NOT NULL,
    severity VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    parent_zone_id UUID REFERENCES geo_core.geo_zones(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);

CREATE TABLE geo_core.geo_fences (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    boundary GEOMETRY(Polygon, 4326) NOT NULL,
    radius_meters DOUBLE PRECISION NOT NULL,
    center GEOMETRY(Point, 4326) NOT NULL,
    fence_type VARCHAR(50) NOT NULL,
    owner_entity_id UUID NOT NULL,
    owner_entity_type VARCHAR(500) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Schema: geo_tracking ─────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_tracking;

CREATE TABLE geo_tracking.tracked_entities (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(500) NOT NULL,
    external_entity_id UUID NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    last_known_location GEOMETRY(Point, 4326) NOT NULL,
    last_speed DOUBLE PRECISION DEFAULT 0,
    last_heading DOUBLE PRECISION DEFAULT 0,
    status VARCHAR(50) NOT NULL,
    last_updated_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geo_tracking.location_history (
    id UUID PRIMARY KEY,
    tracked_entity_id UUID NOT NULL REFERENCES geo_tracking.tracked_entities(id),
    location GEOMETRY(Point, 4326) NOT NULL,
    speed DOUBLE PRECISION DEFAULT 0,
    heading DOUBLE PRECISION DEFAULT 0,
    accuracy DOUBLE PRECISION DEFAULT 0,
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE geo_tracking.tracking_sessions (
    id UUID PRIMARY KEY,
    tracked_entity_id UUID NOT NULL REFERENCES geo_tracking.tracked_entities(id),
    path GEOMETRY(LineString, 4326) NOT NULL,
    total_distance_meters DOUBLE PRECISION DEFAULT 0,
    session_status VARCHAR(50) NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    ended_at TIMESTAMPTZ
);

-- ─── Schema: geo_incidents ────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_incidents;

CREATE TABLE geo_incidents.incident_zones (
    id UUID PRIMARY KEY,
    incident_id UUID NOT NULL,
    incident_type VARCHAR(500) NOT NULL,
    epicenter_location GEOMETRY(Point, 4326) NOT NULL,
    perimeter_boundary GEOMETRY(Polygon, 4326) NOT NULL,
    initial_radius_meters DOUBLE PRECISION NOT NULL,
    current_radius_meters DOUBLE PRECISION NOT NULL,
    severity VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);

CREATE TABLE geo_incidents.responder_positions (
    id UUID PRIMARY KEY,
    responder_id UUID NOT NULL,
    incident_id UUID,
    location GEOMETRY(Point, 4326) NOT NULL,
    speed DOUBLE PRECISION DEFAULT 0,
    heading DOUBLE PRECISION DEFAULT 0,
    dispatch_status VARCHAR(50) NOT NULL,
    distance_to_incident_meters DOUBLE PRECISION,
    eta_minutes DOUBLE PRECISION,
    reported_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE geo_incidents.dispatch_routes (
    id UUID PRIMARY KEY,
    responder_id UUID NOT NULL,
    incident_id UUID NOT NULL,
    origin GEOMETRY(Point, 4326) NOT NULL,
    destination GEOMETRY(Point, 4326) NOT NULL,
    route_path GEOMETRY(LineString, 4326) NOT NULL,
    distance_meters DOUBLE PRECISION NOT NULL,
    estimated_minutes DOUBLE PRECISION NOT NULL,
    route_status VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Schema: geo_disaster ─────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_disaster;

CREATE TABLE geo_disaster.disaster_zones (
    id UUID PRIMARY KEY,
    disaster_event_id UUID NOT NULL,
    disaster_type VARCHAR(500) NOT NULL,
    affected_area GEOMETRY(MultiPolygon, 4326) NOT NULL,
    epicenter GEOMETRY(Point, 4326) NOT NULL,
    severity VARCHAR(50) NOT NULL,
    estimated_affected_population DOUBLE PRECISION DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);

CREATE TABLE geo_disaster.evacuation_routes (
    id UUID PRIMARY KEY,
    disaster_zone_id UUID NOT NULL REFERENCES geo_disaster.disaster_zones(id),
    route_name VARCHAR(200) NOT NULL,
    path GEOMETRY(LineString, 4326) NOT NULL,
    start_point GEOMETRY(Point, 4326) NOT NULL,
    end_point GEOMETRY(Point, 4326) NOT NULL,
    distance_meters DOUBLE PRECISION NOT NULL,
    estimated_minutes DOUBLE PRECISION NOT NULL,
    capacity_persons INTEGER NOT NULL,
    route_status VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geo_disaster.shelter_locations (
    id UUID PRIMARY KEY,
    disaster_zone_id UUID REFERENCES geo_disaster.disaster_zones(id),
    name VARCHAR(200) NOT NULL,
    address VARCHAR(500) NOT NULL,
    location GEOMETRY(Point, 4326) NOT NULL,
    capacity INTEGER NOT NULL,
    current_occupancy INTEGER DEFAULT 0,
    status VARCHAR(50) NOT NULL,
    has_medical BOOLEAN DEFAULT FALSE,
    has_power BOOLEAN DEFAULT FALSE,
    has_water BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Schema: geo_family ───────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_family;

CREATE TABLE geo_family.family_geofences (
    id UUID PRIMARY KEY,
    family_group_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    boundary GEOMETRY(Polygon, 4326) NOT NULL,
    center GEOMETRY(Point, 4326) NOT NULL,
    radius_meters DOUBLE PRECISION NOT NULL,
    alert_type VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geo_family.family_member_locations (
    id UUID PRIMARY KEY,
    family_group_id UUID NOT NULL,
    member_id UUID NOT NULL,
    location GEOMETRY(Point, 4326) NOT NULL,
    accuracy DOUBLE PRECISION DEFAULT 0,
    is_inside_geofence BOOLEAN DEFAULT FALSE,
    active_geofence_id UUID REFERENCES geo_family.family_geofences(id),
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE geo_family.geofence_events (
    id UUID PRIMARY KEY,
    family_geofence_id UUID NOT NULL REFERENCES geo_family.family_geofences(id),
    member_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    location GEOMETRY(Point, 4326) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL
);

-- ─── Schema: geo_search ───────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_search;

CREATE TABLE geo_search.points_of_interest (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    category VARCHAR(500) NOT NULL,
    address VARCHAR(500),
    location GEOMETRY(Point, 4326) NOT NULL,
    phone VARCHAR(50),
    is_emergency_facility BOOLEAN DEFAULT FALSE,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geo_search.spatial_search_results (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(500) NOT NULL,
    entity_id UUID NOT NULL,
    location GEOMETRY(Point, 4326) NOT NULL,
    distance_meters DOUBLE PRECISION NOT NULL,
    label VARCHAR(500),
    found_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── GiST Spatial Indexes ─────────────────────────────────────────────────────

CREATE INDEX idx_geo_locations_location ON geo_core.geo_locations USING GIST (location);
CREATE INDEX idx_geo_zones_boundary ON geo_core.geo_zones USING GIST (boundary);
CREATE INDEX idx_geo_zones_centroid ON geo_core.geo_zones USING GIST (centroid);
CREATE INDEX idx_geo_fences_boundary ON geo_core.geo_fences USING GIST (boundary);
CREATE INDEX idx_geo_fences_center ON geo_core.geo_fences USING GIST (center);

CREATE INDEX idx_tracked_entities_location ON geo_tracking.tracked_entities USING GIST (last_known_location);
CREATE INDEX idx_location_history_location ON geo_tracking.location_history USING GIST (location);
CREATE INDEX idx_tracking_sessions_path ON geo_tracking.tracking_sessions USING GIST (path);

CREATE INDEX idx_incident_zones_epicenter ON geo_incidents.incident_zones USING GIST (epicenter_location);
CREATE INDEX idx_incident_zones_perimeter ON geo_incidents.incident_zones USING GIST (perimeter_boundary);
CREATE INDEX idx_responder_positions_location ON geo_incidents.responder_positions USING GIST (location);
CREATE INDEX idx_dispatch_routes_path ON geo_incidents.dispatch_routes USING GIST (route_path);
CREATE INDEX idx_dispatch_routes_origin ON geo_incidents.dispatch_routes USING GIST (origin);
CREATE INDEX idx_dispatch_routes_destination ON geo_incidents.dispatch_routes USING GIST (destination);

CREATE INDEX idx_disaster_zones_area ON geo_disaster.disaster_zones USING GIST (affected_area);
CREATE INDEX idx_disaster_zones_epicenter ON geo_disaster.disaster_zones USING GIST (epicenter);
CREATE INDEX idx_evacuation_routes_path ON geo_disaster.evacuation_routes USING GIST (path);
CREATE INDEX idx_shelter_locations_location ON geo_disaster.shelter_locations USING GIST (location);

CREATE INDEX idx_family_geofences_boundary ON geo_family.family_geofences USING GIST (boundary);
CREATE INDEX idx_family_geofences_center ON geo_family.family_geofences USING GIST (center);
CREATE INDEX idx_family_member_locations_location ON geo_family.family_member_locations USING GIST (location);
CREATE INDEX idx_geofence_events_location ON geo_family.geofence_events USING GIST (location);

CREATE INDEX idx_poi_location ON geo_search.points_of_interest USING GIST (location);
CREATE INDEX idx_search_results_location ON geo_search.spatial_search_results USING GIST (location);

-- ─── B-Tree Indexes ───────────────────────────────────────────────────────────

CREATE INDEX idx_geo_locations_recorded ON geo_core.geo_locations (recorded_at);
CREATE INDEX idx_geo_locations_source ON geo_core.geo_locations (source_type, source_id);
CREATE INDEX idx_geo_zones_type ON geo_core.geo_zones (zone_type);
CREATE INDEX idx_geo_zones_active ON geo_core.geo_zones (is_active);
CREATE INDEX idx_geo_fences_owner ON geo_core.geo_fences (owner_entity_id);

CREATE INDEX idx_tracked_entities_external ON geo_tracking.tracked_entities (external_entity_id);
CREATE INDEX idx_tracked_entities_type ON geo_tracking.tracked_entities (entity_type);
CREATE INDEX idx_tracked_entities_status ON geo_tracking.tracked_entities (status);
CREATE INDEX idx_location_history_entity ON geo_tracking.location_history (tracked_entity_id, recorded_at);

CREATE INDEX idx_incident_zones_incident ON geo_incidents.incident_zones (incident_id);
CREATE INDEX idx_incident_zones_active ON geo_incidents.incident_zones (is_active);
CREATE INDEX idx_responder_positions_responder ON geo_incidents.responder_positions (responder_id);
CREATE INDEX idx_responder_positions_incident ON geo_incidents.responder_positions (incident_id);
CREATE INDEX idx_dispatch_routes_responder ON geo_incidents.dispatch_routes (responder_id);
CREATE INDEX idx_dispatch_routes_incident ON geo_incidents.dispatch_routes (incident_id);

CREATE INDEX idx_disaster_zones_event ON geo_disaster.disaster_zones (disaster_event_id);
CREATE INDEX idx_disaster_zones_active ON geo_disaster.disaster_zones (is_active);
CREATE INDEX idx_evacuation_routes_zone ON geo_disaster.evacuation_routes (disaster_zone_id);
CREATE INDEX idx_shelter_locations_zone ON geo_disaster.shelter_locations (disaster_zone_id);
CREATE INDEX idx_shelter_locations_status ON geo_disaster.shelter_locations (status);

CREATE INDEX idx_family_geofences_group ON geo_family.family_geofences (family_group_id);
CREATE INDEX idx_family_member_locations_member ON geo_family.family_member_locations (member_id, recorded_at);
CREATE INDEX idx_geofence_events_fence ON geo_family.geofence_events (family_geofence_id);
CREATE INDEX idx_geofence_events_member ON geo_family.geofence_events (member_id);

CREATE INDEX idx_poi_category ON geo_search.points_of_interest (category);
CREATE INDEX idx_poi_emergency ON geo_search.points_of_interest (is_emergency_facility);
CREATE INDEX idx_search_results_entity ON geo_search.spatial_search_results (entity_type, entity_id);

-- ─── Schema: geo_intel ────────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS geo_intel;

CREATE TABLE geo_intel.intel_entries (
    id UUID PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    summary VARCHAR(500) NOT NULL,
    source_type VARCHAR(500) NOT NULL,
    source_url VARCHAR(2048) NOT NULL,
    source_name VARCHAR(200) NOT NULL,
    location GEOMETRY(Point, 4326) NOT NULL,
    radius_meters DOUBLE PRECISION NOT NULL DEFAULT 0,
    category VARCHAR(50) NOT NULL,
    threat_level VARCHAR(50) NOT NULL,
    confidence_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    tags JSONB NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    published_at TIMESTAMPTZ NOT NULL,
    ingested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);

CREATE TABLE geo_intel.intel_inferences (
    id UUID PRIMARY KEY,
    location GEOMETRY(Point, 4326) NOT NULL,
    radius_meters DOUBLE PRECISION NOT NULL DEFAULT 0,
    category VARCHAR(50) NOT NULL,
    threat_level VARCHAR(50) NOT NULL,
    summary VARCHAR(500) NOT NULL,
    confidence_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    supporting_entry_count INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);

CREATE INDEX idx_intel_entries_location ON geo_intel.intel_entries USING GIST (location);
CREATE INDEX idx_intel_entries_category ON geo_intel.intel_entries (category);
CREATE INDEX idx_intel_entries_threat_level ON geo_intel.intel_entries (threat_level);
CREATE INDEX idx_intel_entries_published ON geo_intel.intel_entries (published_at);
CREATE INDEX idx_intel_entries_ingested ON geo_intel.intel_entries (ingested_at);
CREATE INDEX idx_intel_entries_expires ON geo_intel.intel_entries (expires_at);
CREATE INDEX idx_intel_entries_active ON geo_intel.intel_entries (is_active);

CREATE INDEX idx_intel_inferences_location ON geo_intel.intel_inferences USING GIST (location);
CREATE INDEX idx_intel_inferences_category ON geo_intel.intel_inferences (category);
CREATE INDEX idx_intel_inferences_threat_level ON geo_intel.intel_inferences (threat_level);
CREATE INDEX idx_intel_inferences_generated ON geo_intel.intel_inferences (generated_at);
CREATE INDEX idx_intel_inferences_expires ON geo_intel.intel_inferences (expires_at);
CREATE INDEX idx_intel_inferences_active ON geo_intel.intel_inferences (is_active);

-- ─── Spatial Functions ────────────────────────────────────────────────────────

-- 1. Find nearest N entities of a given type within radius
CREATE OR REPLACE FUNCTION geo_search.find_nearest(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_entity_type VARCHAR, p_count INTEGER DEFAULT 10,
    p_max_radius_meters DOUBLE PRECISION DEFAULT 10000
)
RETURNS TABLE(entity_id UUID, entity_type VARCHAR, label VARCHAR, distance_meters DOUBLE PRECISION)
LANGUAGE SQL STABLE AS $$
    SELECT te.external_entity_id, te.entity_type, te.display_name,
           ST_Distance(te.last_known_location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters
    FROM geo_tracking.tracked_entities te
    WHERE te.entity_type = p_entity_type
      AND te.status != 'Offline'
      AND ST_DWithin(te.last_known_location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_max_radius_meters)
    ORDER BY distance_meters
    LIMIT p_count;
$$;

-- 2. Find all entities within radius
CREATE OR REPLACE FUNCTION geo_search.find_within_radius(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_radius_meters DOUBLE PRECISION,
    p_entity_type VARCHAR DEFAULT NULL
)
RETURNS TABLE(entity_id UUID, entity_type VARCHAR, label VARCHAR, distance_meters DOUBLE PRECISION)
LANGUAGE SQL STABLE AS $$
    SELECT te.external_entity_id, te.entity_type, te.display_name,
           ST_Distance(te.last_known_location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters
    FROM geo_tracking.tracked_entities te
    WHERE te.status != 'Offline'
      AND (p_entity_type IS NULL OR te.entity_type = p_entity_type)
      AND ST_DWithin(te.last_known_location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_radius_meters)
    ORDER BY distance_meters;
$$;

-- 3. Check if point is inside any active incident zone
CREATE OR REPLACE FUNCTION geo_incidents.point_in_incident_zone(p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION)
RETURNS TABLE(zone_id UUID, incident_id UUID, severity VARCHAR)
LANGUAGE SQL STABLE AS $$
    SELECT iz.id, iz.incident_id, iz.severity
    FROM geo_incidents.incident_zones iz
    WHERE iz.is_active = TRUE
      AND ST_Contains(iz.perimeter_boundary, ST_MakePoint(p_lon, p_lat));
$$;

-- 4. Calculate distance between two points (meters)
CREATE OR REPLACE FUNCTION geo_core.distance_meters(
    p_lon1 DOUBLE PRECISION, p_lat1 DOUBLE PRECISION,
    p_lon2 DOUBLE PRECISION, p_lat2 DOUBLE PRECISION
)
RETURNS DOUBLE PRECISION
LANGUAGE SQL IMMUTABLE AS $$
    SELECT ST_Distance(ST_MakePoint(p_lon1, p_lat1)::geography, ST_MakePoint(p_lon2, p_lat2)::geography);
$$;

-- 5. Create circular zone boundary from center + radius
CREATE OR REPLACE FUNCTION geo_core.make_circle(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_radius_meters DOUBLE PRECISION
)
RETURNS GEOMETRY(Polygon, 4326)
LANGUAGE SQL IMMUTABLE AS $$
    SELECT ST_SetSRID(
        ST_Buffer(ST_MakePoint(p_lon, p_lat)::geography, p_radius_meters)::geometry,
        4326
    );
$$;

-- 6. Find nearest shelters with capacity
CREATE OR REPLACE FUNCTION geo_disaster.find_nearest_shelters(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_count INTEGER DEFAULT 5, p_max_radius_meters DOUBLE PRECISION DEFAULT 50000
)
RETURNS TABLE(shelter_id UUID, name VARCHAR, distance_meters DOUBLE PRECISION, available_capacity INTEGER)
LANGUAGE SQL STABLE AS $$
    SELECT sl.id, sl.name,
           ST_Distance(sl.location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters,
           (sl.capacity - sl.current_occupancy) AS available_capacity
    FROM geo_disaster.shelter_locations sl
    WHERE sl.status = 'Open'
      AND sl.current_occupancy < sl.capacity
      AND ST_DWithin(sl.location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_max_radius_meters)
    ORDER BY distance_meters
    LIMIT p_count;
$$;

-- 7. Find responders expanding radius (start small, expand until N found)
CREATE OR REPLACE FUNCTION geo_incidents.find_responders_expanding(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_target_count INTEGER DEFAULT 10,
    p_initial_radius DOUBLE PRECISION DEFAULT 1000,
    p_max_radius DOUBLE PRECISION DEFAULT 50000,
    p_expand_factor DOUBLE PRECISION DEFAULT 2.0
)
RETURNS TABLE(responder_id UUID, distance_meters DOUBLE PRECISION)
LANGUAGE PLPGSQL STABLE AS $$
DECLARE
    v_radius DOUBLE PRECISION := p_initial_radius;
    v_count INTEGER := 0;
BEGIN
    LOOP
        SELECT COUNT(*) INTO v_count
        FROM geo_incidents.responder_positions rp
        WHERE rp.dispatch_status = 'Available'
          AND ST_DWithin(rp.location::geography, ST_MakePoint(p_lon, p_lat)::geography, v_radius);

        IF v_count >= p_target_count OR v_radius >= p_max_radius THEN
            RETURN QUERY
                SELECT rp.responder_id,
                       ST_Distance(rp.location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters
                FROM geo_incidents.responder_positions rp
                WHERE rp.dispatch_status = 'Available'
                  AND ST_DWithin(rp.location::geography, ST_MakePoint(p_lon, p_lat)::geography, v_radius)
                ORDER BY distance_meters
                LIMIT p_target_count;
            RETURN;
        END IF;

        v_radius := v_radius * p_expand_factor;
    END LOOP;
END;
$$;

-- 8. Check geofence entry/exit
CREATE OR REPLACE FUNCTION geo_family.check_geofence(
    p_member_id UUID, p_family_group_id UUID,
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION
)
RETURNS TABLE(geofence_id UUID, event_type VARCHAR)
LANGUAGE SQL STABLE AS $$
    SELECT fg.id,
           CASE WHEN ST_Contains(fg.boundary, ST_MakePoint(p_lon, p_lat)) THEN 'Entered' ELSE 'Exited' END
    FROM geo_family.family_geofences fg
    WHERE fg.family_group_id = p_family_group_id
      AND fg.is_active = TRUE;
$$;

-- 9. Get evacuation routes near a point, ordered by distance
CREATE OR REPLACE FUNCTION geo_disaster.find_evac_routes(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_max_distance DOUBLE PRECISION DEFAULT 20000
)
RETURNS TABLE(route_id UUID, route_name VARCHAR, distance_meters DOUBLE PRECISION, status VARCHAR)
LANGUAGE SQL STABLE AS $$
    SELECT er.id, er.route_name,
           ST_Distance(er.start_point::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters,
           er.route_status
    FROM geo_disaster.evacuation_routes er
    WHERE er.route_status != 'Closed'
      AND ST_DWithin(er.start_point::geography, ST_MakePoint(p_lon, p_lat)::geography, p_max_distance)
    ORDER BY distance_meters;
$$;

-- 10. Aggregate entities per zone
CREATE OR REPLACE FUNCTION geo_incidents.count_entities_in_zone(p_zone_id UUID)
RETURNS TABLE(entity_type VARCHAR, entity_count BIGINT)
LANGUAGE SQL STABLE AS $$
    SELECT te.entity_type, COUNT(*) AS entity_count
    FROM geo_tracking.tracked_entities te
    JOIN geo_incidents.incident_zones iz ON iz.id = p_zone_id
    WHERE te.status != 'Offline'
      AND ST_Contains(iz.perimeter_boundary, te.last_known_location)
    GROUP BY te.entity_type;
$$;

-- 11. Calculate route bearing
CREATE OR REPLACE FUNCTION geo_core.bearing(
    p_lon1 DOUBLE PRECISION, p_lat1 DOUBLE PRECISION,
    p_lon2 DOUBLE PRECISION, p_lat2 DOUBLE PRECISION
)
RETURNS DOUBLE PRECISION
LANGUAGE SQL IMMUTABLE AS $$
    SELECT degrees(ST_Azimuth(ST_MakePoint(p_lon1, p_lat1)::geography, ST_MakePoint(p_lon2, p_lat2)::geography));
$$;

-- 12. Find POIs near a point by category
CREATE OR REPLACE FUNCTION geo_search.find_pois(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_category VARCHAR DEFAULT NULL,
    p_count INTEGER DEFAULT 10, p_max_radius DOUBLE PRECISION DEFAULT 5000
)
RETURNS TABLE(poi_id UUID, name VARCHAR, category VARCHAR, distance_meters DOUBLE PRECISION)
LANGUAGE SQL STABLE AS $$
    SELECT poi.id, poi.name, poi.category,
           ST_Distance(poi.location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters
    FROM geo_search.points_of_interest poi
    WHERE (p_category IS NULL OR poi.category = p_category)
      AND ST_DWithin(poi.location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_max_radius)
    ORDER BY distance_meters
    LIMIT p_count;
$$;

-- 13. Query intel entries near a point — supports threat/category filtering
CREATE OR REPLACE FUNCTION geo_intel.find_intel_near(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_radius_meters DOUBLE PRECISION DEFAULT 10000,
    p_category VARCHAR DEFAULT NULL,
    p_min_threat VARCHAR DEFAULT NULL,
    p_count INTEGER DEFAULT 20
)
RETURNS TABLE(
    entry_id UUID, title VARCHAR, category VARCHAR, threat_level VARCHAR,
    confidence_score DOUBLE PRECISION, source_name VARCHAR, distance_meters DOUBLE PRECISION
)
LANGUAGE SQL STABLE AS $$
    SELECT ie.id, ie.title, ie.category, ie.threat_level, ie.confidence_score, ie.source_name,
           ST_Distance(ie.location::geography, ST_MakePoint(p_lon, p_lat)::geography) AS distance_meters
    FROM geo_intel.intel_entries ie
    WHERE ie.is_active = TRUE
      AND (ie.expires_at IS NULL OR ie.expires_at > NOW())
      AND (p_category IS NULL OR ie.category = p_category)
      AND (p_min_threat IS NULL OR ie.threat_level >= p_min_threat)
      AND ST_DWithin(ie.location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_radius_meters)
    ORDER BY ie.threat_level DESC, ie.confidence_score DESC, distance_meters
    LIMIT p_count;
$$;

-- 14. Get active inferences near a point
CREATE OR REPLACE FUNCTION geo_intel.find_inferences_near(
    p_lon DOUBLE PRECISION, p_lat DOUBLE PRECISION,
    p_radius_meters DOUBLE PRECISION DEFAULT 10000
)
RETURNS TABLE(
    inference_id UUID, category VARCHAR, threat_level VARCHAR,
    confidence_score DOUBLE PRECISION, summary VARCHAR,
    supporting_entry_count INTEGER, generated_at TIMESTAMPTZ
)
LANGUAGE SQL STABLE AS $$
    SELECT ii.id, ii.category, ii.threat_level, ii.confidence_score,
           ii.summary, ii.supporting_entry_count, ii.generated_at
    FROM geo_intel.intel_inferences ii
    WHERE ii.is_active = TRUE
      AND (ii.expires_at IS NULL OR ii.expires_at > NOW())
      AND ST_DWithin(ii.location::geography, ST_MakePoint(p_lon, p_lat)::geography, p_radius_meters)
    ORDER BY ii.threat_level DESC, ii.confidence_score DESC;
$$;
