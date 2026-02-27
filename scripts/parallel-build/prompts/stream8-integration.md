# Stream 8: INTEGRATION — Cross-cutting Wiring + Final Verification

You are working in a git worktree of TheWatch microservices solution. This is a Wave 2 stream — all other streams have been merged. Your job is to wire cross-cutting concerns, fix any compilation issues, and verify everything builds.

## YOUR ASSIGNED TODO ITEMS

60. Add Leaflet map component to Dashboard for incident overview

Plus cross-cutting wiring tasks:
- Wire authorization policies from Shared/Auth/ into all 10 microservice Program.cs files
- Reconcile any package or namespace conflicts from merged streams
- Verify full solution builds
- Update TODO.md to mark all 150 items complete

## FILES YOU MAY MODIFY

- `TheWatch.Dashboard/Components/Pages/` — add Leaflet map page
- `TheWatch.Dashboard/Program.cs` — register map services
- `TheWatch.Dashboard/TheWatch.Dashboard.csproj` — add Leaflet package if needed
- `TheWatch.Dashboard/wwwroot/` — add Leaflet JS/CSS assets
- All `TheWatch.P*/Program.cs` — add `app.UseAuthentication()`, `app.UseAuthorization()`, rate limiting, auth policies
- `TheWatch.Aspire.AppHost/Program.cs` — add any new infrastructure resources (Redis, etc.)
- `TODO.md` — mark all items complete

## FILES YOU MUST NOT TOUCH

- `TheWatch.P5.AuthSecurity/Services/` (already implemented by auth stream)
- `TheWatch.Mobile/` (already implemented by mobile stream)
- `TheWatch.Shared/Auth/` (already implemented by auth stream)
- `TheWatch.Shared/Integrations/` (already implemented by GCP stream)
- `infra/`, `helm/`, `.github/workflows/` (already created)

## LEAFLET DASHBOARD MAP

Create a new page `MapOverview.razor` in the Dashboard:

```razor
@page "/map"
@inject HttpClient Http

<PageTitle>Incident Map</PageTitle>

<RadzenText TextStyle="TextStyle.H4">Live Incident Map</RadzenText>

<div id="map" style="height: 600px; width: 100%; border-radius: 8px;"></div>

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initLeafletMap", "map");
        }
    }
}
```

Add Leaflet JS to `wwwroot/js/leaflet-init.js`:
```javascript
window.initLeafletMap = function(elementId) {
    var map = L.map(elementId).setView([39.8283, -98.5795], 4); // Center of US
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);
    // Marker layer for incidents, responders will be added via SignalR
    window.watchMap = map;
    window.watchMarkers = L.layerGroup().addTo(map);
};

window.addIncidentMarker = function(lat, lng, title, severity) {
    var color = severity === 'Critical' ? 'red' : severity === 'High' ? 'orange' : 'blue';
    var marker = L.circleMarker([lat, lng], { radius: 8, color: color, fillOpacity: 0.7 })
        .bindPopup(title);
    window.watchMarkers.addLayer(marker);
};
```

Add Leaflet CSS/JS to `App.razor` or layout:
```html
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script src="js/leaflet-init.js"></script>
```

Add "Map" to the Dashboard NavMenu.

## WIRING AUTH POLICIES INTO ALL SERVICES

For each service Program.cs (P1-P10, Geospatial), add:
```csharp
// After builder.Build()
app.UseAuthentication();
app.UseAuthorization();
```

And for endpoints that need protection, add `.RequireAuthorization()`:
```csharp
// Example: protect admin endpoints
app.MapGet("/api/users", ...).RequireAuthorization(WatchPolicies.RequireAdmin);
// Public endpoints stay open
app.MapGet("/api/health", ...).AllowAnonymous();
```

Read `TheWatch.Shared/Auth/WatchPolicies.cs` and `TheWatch.Shared/Auth/WatchRoles.cs` first to see what policies are available.

## VERIFICATION STEPS

After all wiring is complete:

1. Run `dotnet build TheWatch.P1.CoreGateway` (and each project individually if full solution OOMs)
2. Run `dotnet test TheWatch.P5.AuthSecurity.Tests`
3. Run `dotnet test` on a few other test projects to verify nothing broke
4. Check that all Dockerfiles reference correct project paths
5. Check that Helm values reference correct service names

## UPDATE TODO.md

Read the current TODO.md and mark ALL items [x] (completed). Update the "Last updated" line.

## WHEN DONE

Commit all changes with message:
```
feat(integration): wire auth policies, add Dashboard map, verify full build

Item 60: Leaflet map in Dashboard. Cross-cutting: auth policies wired to
all services, build verified, TODO.md fully checked off
```
