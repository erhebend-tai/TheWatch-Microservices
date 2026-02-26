// TheWatch — Leaflet map interop for Blazor (MAUI + Dashboard)
// Uses OpenStreetMap tiles (no API key required)

window.WatchMap = {
    _maps: {},
    _markers: {},
    _zones: {},
    _userMarker: null,

    // Initialize a Leaflet map in the given container element
    init: function (mapId, lat, lng, zoom) {
        if (this._maps[mapId]) {
            this._maps[mapId].remove();
        }

        const map = L.map(mapId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);

        this._maps[mapId] = map;
        this._markers[mapId] = {};
        this._zones[mapId] = {};

        // Fix tile rendering in dynamic containers
        setTimeout(() => map.invalidateSize(), 200);

        return true;
    },

    // Invalidate size (call after container resize or visibility change)
    invalidateSize: function (mapId) {
        const map = this._maps[mapId];
        if (map) map.invalidateSize();
    },

    // Set map view
    setView: function (mapId, lat, lng, zoom) {
        const map = this._maps[mapId];
        if (map) map.setView([lat, lng], zoom);
    },

    // Fly to location with animation
    flyTo: function (mapId, lat, lng, zoom) {
        const map = this._maps[mapId];
        if (map) map.flyTo([lat, lng], zoom);
    },

    // === Markers ===

    addMarker: function (mapId, markerId, lat, lng, iconType, tooltip, popupHtml) {
        const map = this._maps[mapId];
        if (!map) return;

        // Remove existing marker with same ID
        if (this._markers[mapId][markerId]) {
            map.removeLayer(this._markers[mapId][markerId]);
        }

        const icon = this._createIcon(iconType);
        const marker = L.marker([lat, lng], { icon: icon }).addTo(map);

        if (tooltip) marker.bindTooltip(tooltip, { permanent: false });
        if (popupHtml) marker.bindPopup(popupHtml);

        this._markers[mapId][markerId] = marker;
    },

    moveMarker: function (mapId, markerId, lat, lng, animate) {
        const marker = this._markers[mapId]?.[markerId];
        if (!marker) return;

        if (animate) {
            // Smooth movement over 1 second
            const start = marker.getLatLng();
            const end = L.latLng(lat, lng);
            const duration = 1000;
            const startTime = performance.now();

            const animateStep = (timestamp) => {
                const elapsed = timestamp - startTime;
                const t = Math.min(elapsed / duration, 1);
                const currentLat = start.lat + (end.lat - start.lat) * t;
                const currentLng = start.lng + (end.lng - start.lng) * t;
                marker.setLatLng([currentLat, currentLng]);
                if (t < 1) requestAnimationFrame(animateStep);
            };
            requestAnimationFrame(animateStep);
        } else {
            marker.setLatLng([lat, lng]);
        }
    },

    removeMarker: function (mapId, markerId) {
        const map = this._maps[mapId];
        const marker = this._markers[mapId]?.[markerId];
        if (map && marker) {
            map.removeLayer(marker);
            delete this._markers[mapId][markerId];
        }
    },

    updateMarkerPopup: function (mapId, markerId, popupHtml) {
        const marker = this._markers[mapId]?.[markerId];
        if (marker) marker.setPopupContent(popupHtml);
    },

    // === Zones (Circles & Polygons) ===

    addCircleZone: function (mapId, zoneId, lat, lng, radiusMeters, color, fillOpacity, popupHtml) {
        const map = this._maps[mapId];
        if (!map) return;

        if (this._zones[mapId][zoneId]) {
            map.removeLayer(this._zones[mapId][zoneId]);
        }

        const circle = L.circle([lat, lng], {
            radius: radiusMeters,
            color: color || '#e74c3c',
            fillColor: color || '#e74c3c',
            fillOpacity: fillOpacity || 0.15,
            weight: 2
        }).addTo(map);

        if (popupHtml) circle.bindPopup(popupHtml);
        this._zones[mapId][zoneId] = circle;
    },

    removeZone: function (mapId, zoneId) {
        const map = this._maps[mapId];
        const zone = this._zones[mapId]?.[zoneId];
        if (map && zone) {
            map.removeLayer(zone);
            delete this._zones[mapId][zoneId];
        }
    },

    // === User Location ===

    setUserLocation: function (mapId, lat, lng) {
        const map = this._maps[mapId];
        if (!map) return;

        if (this._userMarker) {
            this._userMarker.setLatLng([lat, lng]);
        } else {
            const icon = L.divIcon({
                className: 'watch-user-marker',
                html: '<div class="user-dot"><div class="user-pulse"></div></div>',
                iconSize: [20, 20],
                iconAnchor: [10, 10]
            });
            this._userMarker = L.marker([lat, lng], { icon: icon, zIndexOffset: 1000 }).addTo(map);
            this._userMarker.bindTooltip('You', { permanent: false });
        }
    },

    // === Fit Bounds ===

    fitAllMarkers: function (mapId, padding) {
        const map = this._maps[mapId];
        const markers = this._markers[mapId];
        if (!map || !markers) return;

        const group = L.featureGroup(Object.values(markers));
        if (group.getLayers().length > 0) {
            map.fitBounds(group.getBounds().pad(padding || 0.1));
        }
    },

    fitBounds: function (mapId, southLat, westLng, northLat, eastLng) {
        const map = this._maps[mapId];
        if (map) {
            map.fitBounds([[southLat, westLng], [northLat, eastLng]]);
        }
    },

    // === Cleanup ===

    clearAll: function (mapId) {
        const map = this._maps[mapId];
        if (!map) return;

        Object.values(this._markers[mapId] || {}).forEach(m => map.removeLayer(m));
        Object.values(this._zones[mapId] || {}).forEach(z => map.removeLayer(z));
        this._markers[mapId] = {};
        this._zones[mapId] = {};
    },

    destroy: function (mapId) {
        this.clearAll(mapId);
        const map = this._maps[mapId];
        if (map) {
            map.remove();
            delete this._maps[mapId];
        }
    },

    // === Icon Factory ===

    _createIcon: function (iconType) {
        const colors = {
            'incident': '#e74c3c',
            'responder-available': '#2ecc71',
            'responder-enroute': '#f39c12',
            'responder-onscene': '#3498db',
            'responder-busy': '#95a5a6',
            'shelter': '#9b59b6',
            'user': '#1abc9c',
            'default': '#34495e'
        };

        const icons = {
            'incident': 'warning',
            'responder-available': 'local_police',
            'responder-enroute': 'directions_run',
            'responder-onscene': 'location_on',
            'responder-busy': 'do_not_disturb',
            'shelter': 'night_shelter',
            'user': 'person_pin',
            'default': 'place'
        };

        const color = colors[iconType] || colors['default'];
        const iconName = icons[iconType] || icons['default'];

        return L.divIcon({
            className: 'watch-map-marker',
            html: `<div class="marker-pin" style="background:${color}">
                     <span class="material-symbols-outlined" style="font-size:16px;color:#fff">${iconName}</span>
                   </div>`,
            iconSize: [30, 42],
            iconAnchor: [15, 42],
            popupAnchor: [0, -42]
        });
    }
};
