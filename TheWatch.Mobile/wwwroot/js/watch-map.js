// TheWatch — Multi-provider map interop for Blazor (Dashboard, Admin, MAUI)
//
// Supported tile providers:
//   osm     — OpenStreetMap standard (default, no key required)
//   osm-hot — Humanitarian OpenStreetMap (no key required)
//   tiger   — US Census Bureau TIGER/Web (no key required)
//   custom  — Self-hosted / custom tile server (options.customTileUrl required)
//   azure   — Azure Maps tile service via Leaflet (options.azureKey required)
//   google  — Google Maps JavaScript API (options.googleApiKey required)
//   apple   — Apple MapKit JS (options.appleToken JWT required)

window.WatchMap = {
    _maps: {},        // mapId -> { instance, type }
    _markers: {},     // mapId -> { markerId -> marker/annotation }
    _zones: {},       // mapId -> { zoneId -> circle/overlay }
    _userMarker: {},  // mapId -> user-location marker/annotation

    // ===== Tile layer configurations for Leaflet-based providers =====
    _tileProviders: {
        'osm': {
            url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        },
        'osm-hot': {
            url: 'https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png',
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, ' +
                'Tiles courtesy of <a href="https://hot.openstreetmap.org/">Humanitarian OpenStreetMap Team</a>',
            maxZoom: 19
        },
        'tiger': {
            url: 'https://tigerweb.geo.census.gov/arcgis/rest/services/TIGERweb/tigerWMS_Current/MapServer/tile/{z}/{y}/{x}',
            attribution: 'US Census Bureau <a href="https://tigerweb.geo.census.gov/">TIGER/Web</a>',
            maxZoom: 18
        }
    },

    // ===== Initialize a map in the given container element =====
    // provider: 'osm' | 'osm-hot' | 'tiger' | 'custom' | 'azure' | 'google' | 'apple'
    // options:  provider-specific key/url object (see above)
    init: function (mapId, lat, lng, zoom, provider, options) {
        provider = provider || 'osm';
        options  = options  || {};

        // Destroy any existing map instance in this container
        this.destroy(mapId);

        if (provider === 'google') return this._initGoogle(mapId, lat, lng, zoom, options);
        if (provider === 'apple')  return this._initApple(mapId, lat, lng, zoom, options);

        // ---- Leaflet-based providers (osm, osm-hot, tiger, custom, azure) ----
        if (typeof L === 'undefined') {
            console.error('WatchMap: Leaflet is not loaded.');
            return false;
        }

        const map = L.map(mapId).setView([lat, lng], zoom);

        let tileCfg;
        if (provider === 'azure') {
            const key = options.azureKey || '';
            tileCfg = {
                url: `https://atlas.microsoft.com/map/tile?api-version=2.0&tilesetId=microsoft.base.road` +
                     `&zoom={z}&x={x}&y={y}&tileSize=256&subscription-key=${key}`,
                attribution: '&copy; <a href="https://azure.microsoft.com/services/azure-maps/">Azure Maps</a>',
                maxZoom: 20
            };
        } else if (provider === 'custom') {
            tileCfg = {
                url:         options.customTileUrl     || 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                attribution: options.customAttribution || '&copy; Map contributors',
                maxZoom:     parseInt(options.maxZoom) || 19
            };
        } else {
            tileCfg = this._tileProviders[provider] || this._tileProviders['osm'];
        }

        L.tileLayer(tileCfg.url, {
            attribution: tileCfg.attribution,
            maxZoom:     tileCfg.maxZoom || 19
        }).addTo(map);

        this._maps[mapId]      = { instance: map, type: 'leaflet' };
        this._markers[mapId]   = {};
        this._zones[mapId]     = {};
        this._userMarker[mapId] = null;

        setTimeout(() => map.invalidateSize(), 200);
        return true;
    },

    // ===== Google Maps JavaScript API =====
    _initGoogle: function (mapId, lat, lng, zoom, options) {
        const apiKey = options.googleApiKey || '';
        const self   = this;

        const doInit = function () {
            if (typeof google === 'undefined' || !google.maps) {
                console.error('WatchMap: Google Maps API failed to load. Check Mapping:GoogleMapsApiKey.');
                return false;
            }
            const el = document.getElementById(mapId);
            if (!el) return false;

            const gmap = new google.maps.Map(el, {
                center:           { lat: lat, lng: lng },
                zoom:             zoom,
                mapTypeId:        'roadmap',
                gestureHandling:  'greedy'
            });

            self._maps[mapId]       = { instance: gmap, type: 'google' };
            self._markers[mapId]    = {};
            self._zones[mapId]      = {};
            self._userMarker[mapId] = null;
            return true;
        };

        if (typeof google !== 'undefined' && google.maps) return doInit();

        if (!apiKey) {
            console.warn('WatchMap: GoogleMapsApiKey not set (Mapping:GoogleMapsApiKey). Falling back to OSM.');
            return this._initLeafletFallback(mapId, lat, lng, zoom);
        }

        const cbName = '_wmgcb_' + mapId.replace(/[^a-z0-9]/gi, '_');
        window[cbName] = function () { doInit(); delete window[cbName]; };

        const s = document.createElement('script');
        s.src   = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&callback=${cbName}`;
        s.async = true;
        s.defer = true;
        document.head.appendChild(s);
        return true;
    },

    // ===== Apple MapKit JS =====
    _initApple: function (mapId, lat, lng, zoom, options) {
        const token = options.appleToken || '';
        const self  = this;

        const doInit = function () {
            if (typeof mapkit === 'undefined') {
                console.error('WatchMap: Apple MapKit JS failed to load. Check Mapping:AppleMapsToken.');
                return false;
            }
            const el = document.getElementById(mapId);
            if (!el) return false;

            const span     = self._zoomToSpan(zoom);
            const appleMap = new mapkit.Map(el, {
                region:        new mapkit.CoordinateRegion(
                                   new mapkit.Coordinate(lat, lng),
                                   new mapkit.CoordinateSpan(span, span)),
                showsScale:    mapkit.FeatureVisibility.Visible,
                showsCompass:  mapkit.FeatureVisibility.Visible
            });

            self._maps[mapId]       = { instance: appleMap, type: 'apple' };
            self._markers[mapId]    = {};
            self._zones[mapId]      = {};
            self._userMarker[mapId] = null;
            return true;
        };

        if (typeof mapkit !== 'undefined') return doInit();

        if (!token) {
            console.warn('WatchMap: AppleMapsToken not set (Mapping:AppleMapsToken). Falling back to OSM.');
            return this._initLeafletFallback(mapId, lat, lng, zoom);
        }

        const s        = document.createElement('script');
        s.src          = 'https://cdn.apple-mapkit.com/mk/5.x.x/mapkit.core.js';
        s.crossOrigin  = 'anonymous';
        s.onload = function () {
            mapkit.init({ authorizationCallback: function (done) { done(token); } });
            doInit();
        };
        document.head.appendChild(s);
        return true;
    },

    _initLeafletFallback: function (mapId, lat, lng, zoom) {
        return this.init(mapId, lat, lng, zoom, 'osm', {});
    },

    // Convert Leaflet zoom level to MapKit coordinate span (degrees lat/lng)
    _zoomToSpan: function (zoom) {
        return Math.max(0.001, 180 / Math.pow(2, zoom - 1));
    },

    // ===== Invalidate size =====
    invalidateSize: function (mapId) {
        const m = this._maps[mapId];
        if (!m) return;
        if (m.type === 'leaflet') m.instance.invalidateSize();
        // Google and Apple Maps handle resize events automatically
    },

    // ===== Set view =====
    setView: function (mapId, lat, lng, zoom) {
        const m = this._maps[mapId];
        if (!m) return;
        if (m.type === 'google') {
            m.instance.setCenter({ lat, lng });
            m.instance.setZoom(zoom);
        } else if (m.type === 'apple') {
            const span = this._zoomToSpan(zoom);
            m.instance.setRegionAnimated(new mapkit.CoordinateRegion(
                new mapkit.Coordinate(lat, lng),
                new mapkit.CoordinateSpan(span, span)));
        } else {
            m.instance.setView([lat, lng], zoom);
        }
    },

    // ===== Fly to =====
    flyTo: function (mapId, lat, lng, zoom) {
        const m = this._maps[mapId];
        if (!m) return;
        if (m.type === 'google') {
            m.instance.panTo({ lat, lng });
            m.instance.setZoom(zoom);
        } else if (m.type === 'apple') {
            const span = this._zoomToSpan(zoom);
            m.instance.setRegionAnimated(new mapkit.CoordinateRegion(
                new mapkit.Coordinate(lat, lng),
                new mapkit.CoordinateSpan(span, span)));
        } else {
            m.instance.flyTo([lat, lng], zoom);
        }
    },

    // ===== Markers =====
    addMarker: function (mapId, markerId, lat, lng, iconType, tooltip, popupHtml) {
        const m = this._maps[mapId];
        if (!m) return;

        this.removeMarker(mapId, markerId);

        if (m.type === 'google') {
            const marker = new google.maps.Marker({
                position: { lat, lng },
                map:      m.instance,
                icon:     this._googleIcon(iconType),
                title:    tooltip || ''
            });
            if (popupHtml) {
                const iw = new google.maps.InfoWindow({ content: popupHtml });
                marker.addListener('click', () => iw.open(m.instance, marker));
                marker._iw = iw;
            }
            this._markers[mapId][markerId] = marker;

        } else if (m.type === 'apple') {
            const ann = new mapkit.MarkerAnnotation(
                new mapkit.Coordinate(lat, lng), {
                    color:     this._appleColor(iconType),
                    title:     tooltip || '',
                    glyphText: this._appleGlyph(iconType)
                });
            if (popupHtml) {
                ann.callout = {
                    calloutContentForAnnotation: () => {
                        const el = document.createElement('div');
                        el.innerHTML = popupHtml;
                        return el;
                    }
                };
            }
            m.instance.addAnnotation(ann);
            this._markers[mapId][markerId] = ann;

        } else {
            const icon   = this._leafletIcon(iconType);
            const marker = L.marker([lat, lng], { icon }).addTo(m.instance);
            if (tooltip)  marker.bindTooltip(tooltip, { permanent: false });
            if (popupHtml) marker.bindPopup(popupHtml);
            this._markers[mapId][markerId] = marker;
        }
    },

    moveMarker: function (mapId, markerId, lat, lng, animate) {
        const m      = this._maps[mapId];
        const marker = this._markers[mapId]?.[markerId];
        if (!m || !marker) return;

        if (m.type === 'google') {
            marker.setPosition({ lat, lng });
        } else if (m.type === 'apple') {
            marker.coordinate = new mapkit.Coordinate(lat, lng);
        } else {
            if (animate) {
                const start     = marker.getLatLng();
                const end       = L.latLng(lat, lng);
                const duration  = 1000;
                const startTime = performance.now();
                const step = (ts) => {
                    const t = Math.min((ts - startTime) / duration, 1);
                    marker.setLatLng([
                        start.lat + (end.lat - start.lat) * t,
                        start.lng + (end.lng - start.lng) * t
                    ]);
                    if (t < 1) requestAnimationFrame(step);
                };
                requestAnimationFrame(step);
            } else {
                marker.setLatLng([lat, lng]);
            }
        }
    },

    removeMarker: function (mapId, markerId) {
        const m      = this._maps[mapId];
        const marker = this._markers[mapId]?.[markerId];
        if (!m || !marker) return;

        if (m.type === 'google') {
            if (marker._iw) marker._iw.close();
            marker.setMap(null);
        } else if (m.type === 'apple') {
            m.instance.removeAnnotation(marker);
        } else {
            m.instance.removeLayer(marker);
        }
        delete this._markers[mapId][markerId];
    },

    updateMarkerPopup: function (mapId, markerId, popupHtml) {
        const m      = this._maps[mapId];
        const marker = this._markers[mapId]?.[markerId];
        if (!m || !marker) return;

        if (m.type === 'google') {
            if (marker._iw) marker._iw.setContent(popupHtml);
        } else if (m.type === 'apple') {
            marker.callout = {
                calloutContentForAnnotation: () => {
                    const el = document.createElement('div');
                    el.innerHTML = popupHtml;
                    return el;
                }
            };
        } else {
            marker.setPopupContent(popupHtml);
        }
    },

    // ===== Zones (circles) =====
    addCircleZone: function (mapId, zoneId, lat, lng, radiusMeters, color, fillOpacity, popupHtml) {
        const m = this._maps[mapId];
        if (!m) return;

        this.removeZone(mapId, zoneId);
        color       = color       || '#e74c3c';
        fillOpacity = fillOpacity || 0.15;

        if (m.type === 'google') {
            const circle = new google.maps.Circle({
                center:      { lat, lng },
                radius:      radiusMeters,
                map:         m.instance,
                strokeColor: color,
                strokeWeight: 2,
                fillColor:   color,
                fillOpacity: fillOpacity
            });
            if (popupHtml) {
                const iw = new google.maps.InfoWindow({ content: popupHtml, position: { lat, lng } });
                circle.addListener('click', () => iw.open(m.instance));
                circle._iw = iw;
            }
            this._zones[mapId][zoneId] = circle;

        } else if (m.type === 'apple') {
            const style   = new mapkit.Style({
                strokeColor:  color,
                lineWidth:    2,
                fillColor:    color,
                fillOpacity:  fillOpacity
            });
            const overlay = new mapkit.CircleOverlay(
                new mapkit.Coordinate(lat, lng),
                radiusMeters,
                { style });
            m.instance.addOverlay(overlay);
            this._zones[mapId][zoneId] = overlay;

        } else {
            const circle = L.circle([lat, lng], {
                radius:      radiusMeters,
                color:       color,
                fillColor:   color,
                fillOpacity: fillOpacity,
                weight:      2
            }).addTo(m.instance);
            if (popupHtml) circle.bindPopup(popupHtml);
            this._zones[mapId][zoneId] = circle;
        }
    },

    removeZone: function (mapId, zoneId) {
        const m    = this._maps[mapId];
        const zone = this._zones[mapId]?.[zoneId];
        if (!m || !zone) return;

        if (m.type === 'google') {
            if (zone._iw) zone._iw.close();
            zone.setMap(null);
        } else if (m.type === 'apple') {
            m.instance.removeOverlay(zone);
        } else {
            m.instance.removeLayer(zone);
        }
        delete this._zones[mapId][zoneId];
    },

    // ===== User location =====
    setUserLocation: function (mapId, lat, lng) {
        const m = this._maps[mapId];
        if (!m) return;

        if (m.type === 'google') {
            if (this._userMarker[mapId]) {
                this._userMarker[mapId].setPosition({ lat, lng });
            } else {
                this._userMarker[mapId] = new google.maps.Marker({
                    position:   { lat, lng },
                    map:        m.instance,
                    title:      'You',
                    icon: {
                        path:         google.maps.SymbolPath.CIRCLE,
                        scale:        8,
                        fillColor:    '#1abc9c',
                        fillOpacity:  1,
                        strokeColor:  '#fff',
                        strokeWeight: 2
                    },
                    zIndex: 1000
                });
            }
        } else if (m.type === 'apple') {
            if (this._userMarker[mapId]) {
                this._userMarker[mapId].coordinate = new mapkit.Coordinate(lat, lng);
            } else {
                const ann = new mapkit.MarkerAnnotation(
                    new mapkit.Coordinate(lat, lng), {
                        color:     '#1abc9c',
                        title:     'You',
                        glyphText: '📍'
                    });
                m.instance.addAnnotation(ann);
                this._userMarker[mapId] = ann;
            }
        } else {
            if (this._userMarker[mapId]) {
                this._userMarker[mapId].setLatLng([lat, lng]);
            } else {
                const icon = L.divIcon({
                    className: 'watch-user-marker',
                    html:      '<div class="user-dot"><div class="user-pulse"></div></div>',
                    iconSize:  [20, 20],
                    iconAnchor:[10, 10]
                });
                this._userMarker[mapId] = L.marker([lat, lng], { icon, zIndexOffset: 1000 }).addTo(m.instance);
                this._userMarker[mapId].bindTooltip('You', { permanent: false });
            }
        }
    },

    // ===== Fit all markers =====
    fitAllMarkers: function (mapId, padding) {
        const m       = this._maps[mapId];
        const markers = this._markers[mapId];
        if (!m || !markers) return;

        const list = Object.values(markers);
        if (list.length === 0) return;

        if (m.type === 'google') {
            const bounds = new google.maps.LatLngBounds();
            list.forEach(mk => bounds.extend(mk.getPosition()));
            m.instance.fitBounds(bounds);
        } else if (m.type === 'apple') {
            m.instance.showItems(list, {
                animate:  true,
                padding:  new mapkit.Padding(40, 40, 40, 40)
            });
        } else {
            const group = L.featureGroup(list);
            if (group.getLayers().length > 0) {
                m.instance.fitBounds(group.getBounds().pad(padding || 0.1));
            }
        }
    },

    fitBounds: function (mapId, southLat, westLng, northLat, eastLng) {
        const m = this._maps[mapId];
        if (!m) return;
        if (m.type === 'google') {
            m.instance.fitBounds({ south: southLat, west: westLng, north: northLat, east: eastLng });
        } else if (m.type === 'apple') {
            const region = new mapkit.BoundingRegion(northLat, eastLng, southLat, westLng).toCoordinateRegion();
            m.instance.setRegionAnimated(region);
        } else {
            m.instance.fitBounds([[southLat, westLng], [northLat, eastLng]]);
        }
    },

    // ===== Cleanup =====
    clearAll: function (mapId) {
        Object.keys(this._markers[mapId] || {}).forEach(id => this.removeMarker(mapId, id));
        Object.keys(this._zones[mapId]   || {}).forEach(id => this.removeZone(mapId, id));
        this._markers[mapId] = {};
        this._zones[mapId]   = {};
    },

    destroy: function (mapId) {
        const m = this._maps[mapId];
        if (!m) return;

        // Remove user marker first
        const um = this._userMarker[mapId];
        if (um) {
            if (m.type === 'google') um.setMap(null);
            else if (m.type === 'apple') m.instance.removeAnnotation(um);
            else m.instance.removeLayer(um);
        }
        delete this._userMarker[mapId];

        this.clearAll(mapId);

        if (m.type === 'leaflet') m.instance.remove();
        else if (m.type === 'apple') m.instance.destroy();
        // Google Maps: no explicit destroy method

        delete this._maps[mapId];
    },

    // ===== Icon / colour factories =====

    // Shared colour map for all providers
    _iconColors: {
        'incident':            '#e74c3c',
        'responder-available': '#2ecc71',
        'responder-enroute':   '#f39c12',
        'responder-onscene':   '#3498db',
        'responder-busy':      '#95a5a6',
        'shelter':             '#9b59b6',
        'user':                '#1abc9c',
        'default':             '#34495e'
    },

    _leafletIcon: function (iconType) {
        const icons = {
            'incident':            'warning',
            'responder-available': 'local_police',
            'responder-enroute':   'directions_run',
            'responder-onscene':   'location_on',
            'responder-busy':      'do_not_disturb',
            'shelter':             'night_shelter',
            'user':                'person_pin',
            'default':             'place'
        };
        const color    = this._iconColors[iconType] || this._iconColors['default'];
        const iconName = icons[iconType]             || icons['default'];
        return L.divIcon({
            className:   'watch-map-marker',
            html:        `<div class="marker-pin" style="background:${color}">` +
                         `<span class="material-symbols-outlined" style="font-size:16px;color:#fff">${iconName}</span>` +
                         `</div>`,
            iconSize:    [30, 42],
            iconAnchor:  [15, 42],
            popupAnchor: [0, -42]
        });
    },

    _googleIcon: function (iconType) {
        const color = this._iconColors[iconType] || this._iconColors['default'];
        return {
            path:         google.maps.SymbolPath.CIRCLE,
            scale:        10,
            fillColor:    color,
            fillOpacity:  1,
            strokeColor:  '#ffffff',
            strokeWeight: 2
        };
    },

    _appleColor: function (iconType) {
        return this._iconColors[iconType] || this._iconColors['default'];
    },

    _appleGlyph: function (iconType) {
        const glyphs = {
            'incident':            '⚠',
            'responder-available': '🚓',
            'responder-enroute':   '🏃',
            'responder-onscene':   '📍',
            'responder-busy':      '⛔',
            'shelter':             '🏠',
            'user':                '👤',
            'default':             '📌'
        };
        return glyphs[iconType] || glyphs['default'];
    }
};
