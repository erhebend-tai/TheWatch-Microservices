#
# Invoke-WatchCloudflare.ps1
# Cloudflare Tunnel commands: Initialize, Start, Stop, Health, DNS routing
#

function Initialize-WatchTunnel {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$TunnelName,

        [string]$Domain = 'thewatch.app',

        [string]$CredentialsPath,

        [switch]$SkipDnsRoutes,

        [switch]$Force
    )

    # ── Step 1: Verify cloudflared is installed ──
    Write-WatchHeader "Cloudflare Tunnel Initialization"

    $cloudflared = Get-Command -Name 'cloudflared' -ErrorAction SilentlyContinue
    if (-not $cloudflared) {
        Write-WatchWarning "cloudflared CLI not found in PATH"
        Write-WatchStatus -Label 'Install' -Value 'https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/' -ValueColor Yellow

        if ($IsWindows -or $env:OS -match 'Windows') {
            Write-Host ""
            Write-Host "  Quick install (winget):" -ForegroundColor Gray
            Write-Host "    winget install Cloudflare.cloudflared" -ForegroundColor White
        }
        elseif ($IsMacOS) {
            Write-Host ""
            Write-Host "  Quick install (brew):" -ForegroundColor Gray
            Write-Host "    brew install cloudflare/cloudflare/cloudflared" -ForegroundColor White
        }
        else {
            Write-Host ""
            Write-Host "  Quick install (apt):" -ForegroundColor Gray
            Write-Host "    curl -fsSL https://pkg.cloudflare.com/cloudflare-main.gpg | sudo tee /usr/share/keyrings/cloudflare.gpg > /dev/null" -ForegroundColor White
            Write-Host "    sudo apt update && sudo apt install cloudflared" -ForegroundColor White
        }

        throw "cloudflared is required. Install it and try again."
    }

    $cfVersion = & cloudflared --version 2>&1
    Write-WatchSuccess "cloudflared found: $cfVersion"

    # ── Step 2: Check authentication ──
    Write-WatchStatus -Label 'Auth' -Value 'Checking Cloudflare login status...'

    $tunnelList = & cloudflared tunnel list --output json 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-WatchWarning "Not authenticated with Cloudflare. Launching login..."
        if (-not $PSCmdlet.ShouldProcess('Cloudflare', 'cloudflared login')) { return }
        & cloudflared login
        if ($LASTEXITCODE -ne 0) {
            throw "Cloudflare login failed. Run 'cloudflared login' manually."
        }
        Write-WatchSuccess "Cloudflare login completed"
    }
    else {
        Write-WatchSuccess "Cloudflare authenticated"
    }

    # ── Step 3: Check if tunnel already exists ──
    $existingTunnels = @()
    try {
        $existingTunnels = $tunnelList | ConvertFrom-Json -ErrorAction SilentlyContinue
    }
    catch {}

    $existing = $existingTunnels | Where-Object { $_.name -eq $TunnelName }

    if ($existing -and -not $Force) {
        Write-WatchSuccess "Tunnel '$TunnelName' already exists (ID: $($existing.id))"
        $tunnelId = $existing.id
    }
    else {
        if ($existing -and $Force) {
            Write-WatchWarning "Force flag set — deleting existing tunnel '$TunnelName'"
            if ($PSCmdlet.ShouldProcess($TunnelName, 'cloudflared tunnel delete')) {
                & cloudflared tunnel delete $TunnelName 2>&1
            }
        }

        # Create the tunnel
        if (-not $PSCmdlet.ShouldProcess($TunnelName, 'cloudflared tunnel create')) { return }

        Write-WatchStatus -Label 'Creating' -Value "Tunnel '$TunnelName'..." -ValueColor Cyan
        $createOutput = & cloudflared tunnel create $TunnelName 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create tunnel: $createOutput"
        }

        # Extract tunnel ID from creation output
        if ($createOutput -match '([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})') {
            $tunnelId = $Matches[1]
        }
        else {
            $tunnelId = '<check cloudflared tunnel list>'
        }

        Write-WatchSuccess "Tunnel created: $tunnelId"
    }

    # ── Step 4: Configure DNS routes ──
    if (-not $SkipDnsRoutes) {
        $dnsRoutes = @(
            @{ Subdomain = 'api';       Description = 'API Gateway (P1 CoreGateway)' }
            @{ Subdomain = 'dashboard'; Description = 'Admin Dashboard (Blazor)' }
            @{ Subdomain = 'emergency'; Description = 'Voice Emergency (P2)' }
            @{ Subdomain = 'auth';      Description = 'Auth Service (P5)' }
            @{ Subdomain = 'responder'; Description = 'First Responder (P6)' }
            @{ Subdomain = 'geo';       Description = 'Geospatial Service' }
        )

        Write-Host ""
        Write-WatchHeader "Configuring DNS Routes"

        foreach ($route in $dnsRoutes) {
            $hostname = "$($route.Subdomain).$Domain"
            Write-WatchStatus -Label 'DNS' -Value "$hostname -> $($route.Description)" -ValueColor Cyan

            if ($PSCmdlet.ShouldProcess($hostname, 'cloudflared tunnel route dns')) {
                $routeOutput = & cloudflared tunnel route dns $TunnelName $hostname 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-WatchSuccess "Route: $hostname"
                }
                else {
                    # Route may already exist — not fatal
                    if ($routeOutput -match 'already exists') {
                        Write-WatchStatus -Label 'Skip' -Value "$hostname (already configured)" -ValueColor Yellow
                    }
                    else {
                        Write-WatchWarning "DNS route failed for $hostname`: $routeOutput"
                    }
                }
            }
        }
    }

    # ── Step 5: Generate config file from template ──
    $root = Get-WatchSolutionRoot
    $templatePath = Join-Path -Path $root -ChildPath 'infra/cloudflare/argo-tunnels.yaml'
    $outputPath = Join-Path -Path $root -ChildPath 'infra/cloudflare/tunnel-config.yaml'

    if (Test-Path -Path $templatePath) {
        Write-Host ""
        Write-WatchStatus -Label 'Config' -Value "Generating tunnel config from template..."

        $config = Get-Content -Path $templatePath -Raw
        $config = $config -replace '\{tunnel-id\}', $tunnelId

        if ($CredentialsPath) {
            $config = $config -replace '/etc/cloudflared/\{tunnel-id\}\.json', $CredentialsPath
        }
        else {
            # Default credentials location
            $defaultCreds = if ($IsWindows -or $env:OS -match 'Windows') {
                "$env:USERPROFILE\.cloudflared\$tunnelId.json"
            }
            else {
                "/etc/cloudflared/$tunnelId.json"
            }
            $config = $config -replace '/etc/cloudflared/\{tunnel-id\}\.json', $defaultCreds
        }

        Set-Content -Path $outputPath -Value $config -Force
        Write-WatchSuccess "Config written: $outputPath"
    }

    # ── Summary ──
    Write-Host ""
    Write-WatchHeader "Tunnel Ready"
    Write-WatchStatus -Label 'Tunnel ID'  -Value $tunnelId -ValueColor Green
    Write-WatchStatus -Label 'Name'       -Value $TunnelName -ValueColor White
    Write-WatchStatus -Label 'Domain'     -Value $Domain -ValueColor White
    Write-WatchStatus -Label 'Config'     -Value $outputPath -ValueColor White
    Write-Host ""
    Write-Host "  Start with: " -ForegroundColor Gray -NoNewline
    Write-Host "Start-WatchTunnel -TunnelName $TunnelName" -ForegroundColor Cyan
    Write-Host ""

    [PSCustomObject]@{
        PSTypeName     = 'TheWatch.Tunnel'
        TunnelId       = $tunnelId
        TunnelName     = $TunnelName
        Domain         = $Domain
        ConfigPath     = $outputPath
        Status         = 'Initialized'
    }
}

function Start-WatchTunnel {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string]$TunnelName = 'thewatch-tunnel',

        [string]$ConfigPath,

        [switch]$Background
    )

    $cloudflared = Get-Command -Name 'cloudflared' -ErrorAction SilentlyContinue
    if (-not $cloudflared) {
        throw "cloudflared is not installed. Run Initialize-WatchTunnel first."
    }

    if (-not $ConfigPath) {
        $root = Get-WatchSolutionRoot
        $ConfigPath = Join-Path -Path $root -ChildPath 'infra/cloudflare/tunnel-config.yaml'
    }

    if (-not (Test-Path -Path $ConfigPath)) {
        throw "Tunnel config not found: $ConfigPath. Run Initialize-WatchTunnel first."
    }

    if (-not $PSCmdlet.ShouldProcess($TunnelName, 'cloudflared tunnel run')) { return }

    Write-WatchHeader "Starting Cloudflare Tunnel"
    Write-WatchStatus -Label 'Config' -Value $ConfigPath
    Write-WatchStatus -Label 'Tunnel' -Value $TunnelName -ValueColor Cyan

    if ($Background) {
        Write-WatchStatus -Label 'Mode' -Value 'Background (nohup)' -ValueColor Yellow
        if ($IsWindows -or $env:OS -match 'Windows') {
            Start-Process -FilePath 'cloudflared' -ArgumentList "tunnel --config `"$ConfigPath`" run $TunnelName" -WindowStyle Hidden
        }
        else {
            Start-Process -FilePath 'nohup' -ArgumentList "cloudflared tunnel --config `"$ConfigPath`" run $TunnelName" -RedirectStandardOutput '/dev/null'
        }
        Write-WatchSuccess "Tunnel started in background"
    }
    else {
        Write-WatchStatus -Label 'Mode' -Value 'Foreground (Ctrl+C to stop)' -ValueColor Yellow
        & cloudflared tunnel --config $ConfigPath run $TunnelName
    }
}

function Stop-WatchTunnel {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [string]$TunnelName = 'thewatch-tunnel'
    )

    if (-not $PSCmdlet.ShouldProcess($TunnelName, 'Stop cloudflared tunnel process')) { return }

    Write-WatchHeader "Stopping Cloudflare Tunnel"

    if ($IsWindows -or $env:OS -match 'Windows') {
        $processes = Get-Process -Name 'cloudflared' -ErrorAction SilentlyContinue
        if ($processes) {
            $processes | Stop-Process -Force
            Write-WatchSuccess "Stopped $($processes.Count) cloudflared process(es)"
        }
        else {
            Write-WatchWarning "No cloudflared processes found"
        }
    }
    else {
        $pids = & pgrep -f "cloudflared.*$TunnelName" 2>/dev/null
        if ($pids) {
            & pkill -f "cloudflared.*$TunnelName" 2>/dev/null
            Write-WatchSuccess "Tunnel processes terminated"
        }
        else {
            Write-WatchWarning "No cloudflared processes found for '$TunnelName'"
        }
    }
}

function Test-WatchTunnelHealth {
    [CmdletBinding()]
    [OutputType('TheWatch.TunnelHealth')]
    param(
        [string]$TunnelName = 'thewatch-tunnel',

        [string]$Domain = 'thewatch.app'
    )

    Write-WatchHeader "Cloudflare Tunnel Health Check"

    $results = @()

    # ── 1: cloudflared binary ──
    $cfCmd = Get-Command -Name 'cloudflared' -ErrorAction SilentlyContinue
    $cfInstalled = $null -ne $cfCmd
    if ($cfInstalled) {
        $cfVersion = & cloudflared --version 2>&1
        Write-WatchSuccess "cloudflared installed: $cfVersion"
    }
    else {
        Write-WatchError "cloudflared not installed"
    }
    $results += [PSCustomObject]@{ Check = 'cloudflared-installed'; Status = if ($cfInstalled) { 'Pass' } else { 'Fail' }; Detail = "$cfVersion" }

    # ── 2: Tunnel exists ──
    $tunnelExists = $false
    $tunnelId = ''
    if ($cfInstalled) {
        $listOutput = & cloudflared tunnel list --output json 2>&1
        if ($LASTEXITCODE -eq 0) {
            try {
                $tunnels = $listOutput | ConvertFrom-Json
                $match = $tunnels | Where-Object { $_.name -eq $TunnelName }
                if ($match) {
                    $tunnelExists = $true
                    $tunnelId = $match.id
                    Write-WatchSuccess "Tunnel '$TunnelName' exists (ID: $tunnelId)"
                }
                else {
                    Write-WatchWarning "Tunnel '$TunnelName' not found"
                }
            }
            catch {
                Write-WatchWarning "Could not parse tunnel list"
            }
        }
    }
    $results += [PSCustomObject]@{ Check = 'tunnel-exists'; Status = if ($tunnelExists) { 'Pass' } else { 'Fail' }; Detail = $tunnelId }

    # ── 3: Config file present ──
    $root = Get-WatchSolutionRoot
    $configPath = Join-Path -Path $root -ChildPath 'infra/cloudflare/tunnel-config.yaml'
    $configExists = Test-Path -Path $configPath
    if ($configExists) {
        Write-WatchSuccess "Tunnel config exists: $configPath"
    }
    else {
        Write-WatchWarning "Config not found (run Initialize-WatchTunnel)"
    }
    $results += [PSCustomObject]@{ Check = 'config-file'; Status = if ($configExists) { 'Pass' } else { 'Fail' }; Detail = $configPath }

    # ── 4: DNS resolution for subdomains ──
    $subdomains = @('api', 'dashboard', 'emergency', 'auth', 'responder', 'geo')
    foreach ($sub in $subdomains) {
        $fqdn = "$sub.$Domain"
        try {
            $resolved = Resolve-DnsName -Name $fqdn -ErrorAction Stop 2>$null
            if ($resolved) {
                Write-WatchSuccess "DNS resolves: $fqdn"
                $results += [PSCustomObject]@{ Check = "dns-$sub"; Status = 'Pass'; Detail = ($resolved | Select-Object -First 1).IPAddress }
            }
        }
        catch {
            Write-WatchWarning "DNS not configured: $fqdn"
            $results += [PSCustomObject]@{ Check = "dns-$sub"; Status = 'Warn'; Detail = 'NXDOMAIN or unreachable' }
        }
    }

    # ── 5: Tunnel process running ──
    $processRunning = $false
    if ($IsWindows -or $env:OS -match 'Windows') {
        $processes = Get-Process -Name 'cloudflared' -ErrorAction SilentlyContinue
        $processRunning = $null -ne $processes -and $processes.Count -gt 0
    }
    else {
        $pids = & pgrep -f 'cloudflared' 2>/dev/null
        $processRunning = $null -ne $pids -and $pids.Length -gt 0
    }

    if ($processRunning) {
        Write-WatchSuccess "cloudflared process is running"
    }
    else {
        Write-WatchStatus -Label 'Tunnel' -Value 'Not running (use Start-WatchTunnel)' -ValueColor Yellow
    }
    $results += [PSCustomObject]@{ Check = 'process-running'; Status = if ($processRunning) { 'Pass' } else { 'Info' }; Detail = '' }

    # ── 6: Local service reachability ──
    Write-Host ""
    Write-WatchStatus -Label 'Services' -Value 'Checking local service ports...'

    $portMap = @{
        'CoreGateway (P1)'    = 5101
        'VoiceEmergency (P2)' = 5102
        'AuthSecurity (P5)'   = 5105
        'FirstResponder (P6)' = 5106
        'Geospatial'          = 5111
        'Dashboard'           = 5100
    }

    foreach ($kv in $portMap.GetEnumerator()) {
        $conn = Test-NetConnection -ComputerName 'localhost' -Port $kv.Value -WarningAction SilentlyContinue -InformationLevel Quiet 2>$null
        if ($conn) {
            Write-WatchSuccess "$($kv.Key) :$($kv.Value) reachable"
        }
        else {
            Write-WatchWarning "$($kv.Key) :$($kv.Value) not reachable"
        }
        $results += [PSCustomObject]@{ Check = "port-$($kv.Value)"; Status = if ($conn) { 'Pass' } else { 'Warn' }; Detail = "$($kv.Key)" }
    }

    # ── Summary ──
    $passCount = ($results | Where-Object { $_.Status -eq 'Pass' }).Count
    $failCount = ($results | Where-Object { $_.Status -eq 'Fail' }).Count
    $warnCount = ($results | Where-Object { $_.Status -eq 'Warn' }).Count

    Write-Host ""
    Write-WatchHeader "Tunnel Health Summary"
    Write-WatchStatus -Label 'Pass' -Value $passCount -ValueColor Green
    Write-WatchStatus -Label 'Warn' -Value $warnCount -ValueColor Yellow
    Write-WatchStatus -Label 'Fail' -Value $failCount -ValueColor Red

    $results | ForEach-Object {
        $_.PSObject.TypeNames.Insert(0, 'TheWatch.TunnelHealth')
    }

    return $results
}
