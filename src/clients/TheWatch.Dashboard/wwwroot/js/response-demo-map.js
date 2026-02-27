window.ResponseDemoMap = {
    _map: null,
    _datasource: null,
    _patrolMarker: null,
    _neighborMarker: null,
    _animationId: null,
    _dotNetRef: null,
    _startTime: null,
    _pausedElapsed: 0,
    _isPaused: false,
    _speedMultiplier: 1,
    _patrolArrived: false,
    _neighborArrived: false,
    _lastCallbackTime: 0,

    // Durations in milliseconds (sim time)
    PATROL_DURATION: 540000,   // 9 minutes
    NEIGHBOR_DURATION: 70000,  // 70 seconds

    // Scene coordinates
    RESIDENCE: [-97.7431, 30.2672],
    POLICE_STATION: [-97.7600, 30.2500],
    NEIGHBOR_HOUSE: [-97.7425, 30.2678],

    // Patrol route: ~3.8km through Austin street grid (15+ waypoints)
    PATROL_ROUTE: [
        [-97.7600, 30.2500],  // Police station
        [-97.7595, 30.2510],
        [-97.7580, 30.2520],
        [-97.7565, 30.2535],
        [-97.7550, 30.2545],
        [-97.7535, 30.2555],
        [-97.7520, 30.2565],
        [-97.7510, 30.2575],
        [-97.7500, 30.2590],
        [-97.7490, 30.2600],
        [-97.7480, 30.2615],
        [-97.7470, 30.2625],
        [-97.7465, 30.2635],
        [-97.7455, 30.2645],
        [-97.7450, 30.2655],
        [-97.7445, 30.2660],
        [-97.7440, 30.2665],
        [-97.7435, 30.2670],
        [-97.7431, 30.2672]   // Residence
    ],

    // Neighbor route: ~80m door-to-door jog
    NEIGHBOR_ROUTE: [
        [-97.7425, 30.2678],  // Neighbor house
        [-97.7426, 30.2676],
        [-97.7428, 30.2674],
        [-97.7430, 30.2673],
        [-97.7431, 30.2672]   // Residence
    ],

    init: function (elementId, subscriptionKey, dotNetRef) {
        this._dotNetRef = dotNetRef;

        if (!subscriptionKey) {
            return false;
        }

        this._map = new atlas.Map(elementId, {
            center: this.RESIDENCE,
            zoom: 14,
            style: 'night',
            authOptions: {
                authType: 'subscriptionKey',
                subscriptionKey: subscriptionKey
            }
        });

        var self = this;
        this._map.events.add('ready', function () {
            self._datasource = new atlas.source.DataSource();
            self._map.sources.add(self._datasource);

            self._addRouteLines();
            self._addStaticMarkers();
        });

        return true;
    },

    _addRouteLines: function () {
        // Patrol route (red dashed)
        var patrolLine = new atlas.data.LineString(this.PATROL_ROUTE);
        var patrolFeature = new atlas.data.Feature(patrolLine, { routeType: 'patrol' });
        this._datasource.add(patrolFeature);

        this._map.layers.add(new atlas.layer.LineLayer(this._datasource, 'patrol-route', {
            strokeColor: ['case',
                ['==', ['get', 'routeType'], 'patrol'], '#e74c3c',
                ['==', ['get', 'routeType'], 'neighbor'], '#2ecc71',
                '#888'
            ],
            strokeWidth: 3,
            strokeDashArray: [4, 4],
            filter: ['any',
                ['==', ['get', 'routeType'], 'patrol'],
                ['==', ['get', 'routeType'], 'neighbor']
            ]
        }));

        // Neighbor route (green dashed)
        var neighborLine = new atlas.data.LineString(this.NEIGHBOR_ROUTE);
        var neighborFeature = new atlas.data.Feature(neighborLine, { routeType: 'neighbor' });
        this._datasource.add(neighborFeature);
    },

    _addStaticMarkers: function () {
        // Residence marker (pulsing red)
        var residenceHtml = '<div class="demo-marker demo-marker-residence"><span class="material-symbols-outlined">emergency</span></div>';
        new atlas.HtmlMarker({
            position: this.RESIDENCE,
            htmlContent: residenceHtml,
            anchor: 'center'
        }).addTo(this._map);

        // Police station marker
        var policeHtml = '<div class="demo-marker demo-marker-police"><span class="material-symbols-outlined">local_police</span></div>';
        new atlas.HtmlMarker({
            position: this.POLICE_STATION,
            htmlContent: policeHtml,
            anchor: 'center'
        }).addTo(this._map);

        // Neighbor house marker
        var neighborHtml = '<div class="demo-marker demo-marker-neighbor"><span class="material-symbols-outlined">home</span></div>';
        new atlas.HtmlMarker({
            position: this.NEIGHBOR_HOUSE,
            htmlContent: neighborHtml,
            anchor: 'center'
        }).addTo(this._map);
    },

    _createMovingMarker: function (iconName, cssClass) {
        var html = '<div class="demo-marker ' + cssClass + '"><span class="material-symbols-outlined">' + iconName + '</span></div>';
        return new atlas.HtmlMarker({
            position: [0, 0],
            htmlContent: html,
            anchor: 'center',
            visible: false
        });
    },

    start: function () {
        if (this._animationId) return;

        this._patrolArrived = false;
        this._neighborArrived = false;

        // Create moving markers
        if (this._patrolMarker) {
            this._map.markers.remove(this._patrolMarker);
        }
        if (this._neighborMarker) {
            this._map.markers.remove(this._neighborMarker);
        }

        this._patrolMarker = this._createMovingMarker('directions_car', 'demo-marker-patrol-moving');
        this._neighborMarker = this._createMovingMarker('directions_run', 'demo-marker-neighbor-moving');

        this._patrolMarker.setOptions({ position: this.PATROL_ROUTE[0], visible: true });
        this._neighborMarker.setOptions({ position: this.NEIGHBOR_ROUTE[0], visible: true });

        this._map.markers.add(this._patrolMarker);
        this._map.markers.add(this._neighborMarker);

        this._startTime = performance.now();
        this._pausedElapsed = 0;
        this._isPaused = false;
        this._lastCallbackTime = 0;

        this._animate();
    },

    pause: function () {
        if (this._isPaused || !this._animationId) return;
        this._isPaused = true;
        this._pausedElapsed += (performance.now() - this._startTime) * this._speedMultiplier;
        cancelAnimationFrame(this._animationId);
        this._animationId = null;
    },

    resume: function () {
        if (!this._isPaused) return;
        this._isPaused = false;
        this._startTime = performance.now();
        this._animate();
    },

    reset: function () {
        if (this._animationId) {
            cancelAnimationFrame(this._animationId);
            this._animationId = null;
        }
        this._isPaused = false;
        this._pausedElapsed = 0;
        this._patrolArrived = false;
        this._neighborArrived = false;

        if (this._patrolMarker) {
            this._map.markers.remove(this._patrolMarker);
            this._patrolMarker = null;
        }
        if (this._neighborMarker) {
            this._map.markers.remove(this._neighborMarker);
            this._neighborMarker = null;
        }

        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnResetCallback');
        }
    },

    setSpeed: function (multiplier) {
        if (this._animationId && !this._isPaused) {
            this._pausedElapsed += (performance.now() - this._startTime) * this._speedMultiplier;
            this._startTime = performance.now();
        }
        this._speedMultiplier = multiplier;
    },

    _animate: function () {
        var self = this;
        var now = performance.now();
        var elapsedSim = this._pausedElapsed + (now - this._startTime) * this._speedMultiplier;

        // Patrol progress
        var patrolProgress = Math.min(elapsedSim / this.PATROL_DURATION, 1.0);
        if (!this._patrolArrived) {
            var patrolPos = this._interpolateRoute(this.PATROL_ROUTE, patrolProgress);
            this._patrolMarker.setOptions({ position: patrolPos });
        }

        // Neighbor progress
        var neighborProgress = Math.min(elapsedSim / this.NEIGHBOR_DURATION, 1.0);
        if (!this._neighborArrived) {
            var neighborPos = this._interpolateRoute(this.NEIGHBOR_ROUTE, neighborProgress);
            this._neighborMarker.setOptions({ position: neighborPos });
        }

        // Check arrivals
        if (neighborProgress >= 1.0 && !this._neighborArrived) {
            this._neighborArrived = true;
            this._neighborMarker.setOptions({ position: this.RESIDENCE });
        }
        if (patrolProgress >= 1.0 && !this._patrolArrived) {
            this._patrolArrived = true;
            this._patrolMarker.setOptions({ position: this.RESIDENCE });
        }

        // Throttled callback to Blazor (~10Hz = every 100ms real time)
        if (now - this._lastCallbackTime >= 100 && this._dotNetRef) {
            this._lastCallbackTime = now;
            var patrolEta = Math.max(0, (this.PATROL_DURATION - elapsedSim) / 1000);
            var neighborEta = Math.max(0, (this.NEIGHBOR_DURATION - elapsedSim) / 1000);

            this._dotNetRef.invokeMethodAsync('OnAnimationProgress',
                patrolProgress,
                neighborProgress,
                patrolEta,
                neighborEta,
                this._neighborArrived,
                this._patrolArrived
            );
        }

        // Continue or finish
        if (this._patrolArrived && this._neighborArrived) {
            this._animationId = null;
            if (this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('OnDemoComplete');
            }
            return;
        }

        this._animationId = requestAnimationFrame(function () { self._animate(); });
    },

    _interpolateRoute: function (route, progress) {
        if (progress <= 0) return route[0];
        if (progress >= 1) return route[route.length - 1];

        // Calculate total route length
        var segments = [];
        var totalLength = 0;
        for (var i = 1; i < route.length; i++) {
            var dx = route[i][0] - route[i - 1][0];
            var dy = route[i][1] - route[i - 1][1];
            var len = Math.sqrt(dx * dx + dy * dy);
            segments.push(len);
            totalLength += len;
        }

        // Find position along route at given progress
        var targetDist = progress * totalLength;
        var accumulated = 0;
        for (var i = 0; i < segments.length; i++) {
            if (accumulated + segments[i] >= targetDist) {
                var segProgress = (targetDist - accumulated) / segments[i];
                var lng = route[i][0] + (route[i + 1][0] - route[i][0]) * segProgress;
                var lat = route[i][1] + (route[i + 1][1] - route[i][1]) * segProgress;
                return [lng, lat];
            }
            accumulated += segments[i];
        }
        return route[route.length - 1];
    },

    dispose: function () {
        if (this._animationId) {
            cancelAnimationFrame(this._animationId);
            this._animationId = null;
        }
        if (this._map) {
            this._map.dispose();
            this._map = null;
        }
        this._dotNetRef = null;
    }
};
