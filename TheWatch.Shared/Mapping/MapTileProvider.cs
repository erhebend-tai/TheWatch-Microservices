namespace TheWatch.Shared.Mapping;

/// <summary>
/// Supported map tile providers for the WatchMap component.
/// The string value (lowercase) is passed directly to the JavaScript WatchMap.init() function.
/// </summary>
public enum MapTileProvider
{
    /// <summary>OpenStreetMap standard tiles via Leaflet (no API key required).</summary>
    Osm,

    /// <summary>Humanitarian OpenStreetMap tiles via Leaflet (no API key required).</summary>
    OsmHot,

    /// <summary>US Census Bureau TIGER web tiles via Leaflet (no API key required).</summary>
    Tiger,

    /// <summary>Custom/self-hosted tile server via Leaflet. Requires Mapping:CustomTileUrl.</summary>
    Custom,

    /// <summary>Azure Maps tile service via Leaflet. Requires Azure:MapsSubscriptionKey.</summary>
    Azure,

    /// <summary>Google Maps JavaScript API. Requires Mapping:GoogleMapsApiKey.</summary>
    Google,

    /// <summary>Apple MapKit JS. Requires Mapping:AppleMapsToken (JWT).</summary>
    Apple
}
