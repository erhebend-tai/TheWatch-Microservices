#
# Get-WatchHealth.ps1
# Health and service info commands
#

function Get-WatchHealth {
    [CmdletBinding()]
    [OutputType('TheWatch.HealthStatus')]
    param(
        [string]$BaseUrl
    )

    # Determine base URL prefix for health checks
    if (-not $BaseUrl) {
        $session = Read-WatchSession
        if ($session) {
            # Derive from session — extract protocol + host, strip any path
            $uri = [Uri]$session.BaseUrl
            $hostBase = "$($uri.Scheme)://$($uri.Host)"
        }
        else {
            $hostBase = 'http://localhost'
        }
    }
    else {
        $hostBase = $BaseUrl.TrimEnd('/')
    }

    Write-WatchHeader "TheWatch Service Health"

    # Run health checks in parallel using runspaces
    $runspacePool = [RunspaceFactory]::CreateRunspacePool(1, 12)
    $runspacePool.Open()

    $jobs = @()
    foreach ($svc in $script:WatchServices.GetEnumerator()) {
        $svcName = $svc.Key
        $svcInfo = $svc.Value
        $healthUrl = "${hostBase}:$($svcInfo.Port)/health"

        $ps = [PowerShell]::Create().AddScript({
            param($url, $name, $displayName)
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $result = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 5 -ErrorAction Stop
                $sw.Stop()
                @{
                    Service    = $displayName
                    Name       = $name
                    Status     = if ($result.status) { $result.status } else { 'Healthy' }
                    Url        = $url
                    ResponseMs = $sw.ElapsedMilliseconds
                }
            }
            catch {
                $sw.Stop()
                @{
                    Service    = $displayName
                    Name       = $name
                    Status     = 'Unhealthy'
                    Url        = $url
                    ResponseMs = $sw.ElapsedMilliseconds
                }
            }
        }).AddArgument($healthUrl).AddArgument($svcName).AddArgument($svcInfo.Name)

        $ps.RunspacePool = $runspacePool
        $jobs += @{ PowerShell = $ps; Handle = $ps.BeginInvoke() }
    }

    # Collect results
    $results = foreach ($job in $jobs) {
        $data = $job.PowerShell.EndInvoke($job.Handle)
        $job.PowerShell.Dispose()
        $data
    }

    $runspacePool.Close()
    $runspacePool.Dispose()

    # Output typed objects with color
    foreach ($r in $results | Sort-Object { $_.Name }) {
        $statusColor = switch ($r.Status) {
            'Healthy'   { 'Green' }
            'Degraded'  { 'Yellow' }
            default     { 'Red' }
        }
        Write-WatchStatus -Label $r.Service -Value "$($r.Status) ($($r.ResponseMs)ms)" -ValueColor $statusColor

        [PSCustomObject]@{
            PSTypeName = 'TheWatch.HealthStatus'
            Service    = $r.Service
            Name       = $r.Name
            Status     = $r.Status
            Url        = $r.Url
            ResponseMs = $r.ResponseMs
        }
    }
}

function Get-WatchServiceInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Service,

        [string]$BaseUrl
    )

    if (-not $script:WatchServices.ContainsKey($Service)) {
        throw "Unknown service '$Service'. Valid services: $($script:WatchServices.Keys -join ', ')"
    }

    $svcInfo = $script:WatchServices[$Service]

    if (-not $BaseUrl) {
        $session = Read-WatchSession
        if ($session) {
            $uri = [Uri]$session.BaseUrl
            $hostBase = "$($uri.Scheme)://$($uri.Host)"
        }
        else {
            $hostBase = 'http://localhost'
        }
    }
    else {
        $hostBase = $BaseUrl.TrimEnd('/')
    }

    $infoUrl = "${hostBase}:$($svcInfo.Port)/info"

    try {
        $response = Invoke-RestMethod -Uri $infoUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        $response
    }
    catch {
        throw "Failed to get info from $($svcInfo.Name) at $infoUrl`: $_"
    }
}
