namespace TheWatch.Shared.Mapping;

/// <summary>
/// Configuration for the WatchMap tile provider and associated API keys.
/// Bind from the "Mapping" section in appsettings.json.
///
/// TileProvider choices:
///   osm       – OpenStreetMap standard (default, no key required)
///   osm-hot   – Humanitarian OpenStreetMap (no key required)
///   tiger     – US Census Bureau TIGER/Web (no key required)
///   custom    – Self-hosted / custom tile server (set CustomTileUrl)
///   azure     – Azure Maps tiles (set Azure:MapsSubscriptionKey)
///   google    – Google Maps JavaScript API (set GoogleMapsApiKey)
///   apple     – Apple MapKit JS (set AppleMapsToken)
/// </summary>
public class MapSettings
{
    public const string SectionName = "Mapping";

    /// <summary>
    /// Active tile provider key. Must match one of the JS provider strings.
    /// Defaults to "osm" (OpenStreetMap via Leaflet).
    /// </summary>
    public string TileProvider { get; set; } = "osm";

    /// <summary>
    /// Tile URL template for the "custom" provider.
    /// Example: "https://tiles.example.com/{z}/{x}/{y}.png"
    /// </summary>
    public string? CustomTileUrl { get; set; }

    /// <summary>
    /// Attribution text displayed when using the "custom" provider.
    /// </summary>
    public string? CustomTileAttribution { get; set; }

    /// <summary>
    /// Google Maps JavaScript API key. Required when TileProvider = "google".
    /// Obtain from https://console.cloud.google.com/
    /// </summary>
    public string? GoogleMapsApiKey { get; set; }

    /// <summary>
    /// Apple MapKit JS authorization token (JWT). Required when TileProvider = "apple".
    /// Obtain from https://developer.apple.com/account/resources/authkeys/list
    /// </summary>
    public string? AppleMapsToken { get; set; }
}
