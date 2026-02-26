#
# Invoke-WatchDocker.ps1
# Docker Compose commands: Start, Stop, Restart, Containers, Logs, InitDB
#

function Start-WatchStack {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string[]]$Service,

        [switch]$Build,

        [switch]$ForceRecreate
    )

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    $args = @('compose', 'up', '-d')
    if ($Build) { $args += '--build' }
    if ($ForceRecreate) { $args += '--force-recreate' }
    if ($Service) { $args += $Service }

    $desc = if ($Service) { "services: $($Service -join ', ')" } else { "all services" }

    if (-not $PSCmdlet.ShouldProcess($desc, 'docker compose up')) { return }

    Write-WatchHeader "Starting TheWatch Stack"
    Write-WatchStatus -Label 'Directory' -Value $root

    Push-Location $root
    try {
        & docker @args
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "Stack started ($desc)"
        }
        else {
            Write-WatchError "docker compose up failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function Stop-WatchStack {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [switch]$RemoveVolumes
    )

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    $args = @('compose', 'down')
    if ($RemoveVolumes) {
        Write-WatchWarning "RemoveVolumes will delete all persistent data (databases, caches)!"
        $args += '--volumes'
    }

    $target = if ($RemoveVolumes) { 'stack + volumes' } else { 'stack' }

    if (-not $PSCmdlet.ShouldProcess($target, 'docker compose down')) { return }

    Write-WatchHeader "Stopping TheWatch Stack"

    Push-Location $root
    try {
        & docker @args
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "Stack stopped"
        }
        else {
            Write-WatchError "docker compose down failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function Restart-WatchService {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string[]]$Service
    )

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    foreach ($svc in $Service) {
        if (-not $script:WatchServices.ContainsKey($svc)) {
            Write-WatchWarning "Unknown service '$svc' — skipping. Valid: $($script:WatchServices.Keys -join ', ')"
            continue
        }

        if (-not $PSCmdlet.ShouldProcess($svc, 'docker compose restart')) { continue }

        Write-WatchStatus -Label 'Restarting' -Value $svc -ValueColor Cyan

        Push-Location $root
        try {
            & docker compose restart $svc
            if ($LASTEXITCODE -eq 0) {
                Write-WatchSuccess "$svc restarted"
            }
            else {
                Write-WatchError "$svc restart failed"
            }
        }
        finally {
            Pop-Location
        }
    }
}

function Get-WatchContainer {
    [CmdletBinding()]
    [OutputType('TheWatch.Container')]
    param(
        [switch]$Raw
    )

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    Push-Location $root
    try {
        $output = & docker compose ps --format json 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-WatchError "docker compose ps failed"
            return
        }

        # Parse JSON lines
        $containers = $output | Where-Object { $_ -match '^\{' } | ForEach-Object {
            $_ | ConvertFrom-Json
        }

        if ($Raw) {
            return $containers
        }

        Write-WatchHeader "TheWatch Containers"

        foreach ($c in $containers) {
            $statusColor = switch -Regex ($c.State) {
                'running' { 'Green' }
                'exited'  { 'Red' }
                'paused'  { 'Yellow' }
                default   { 'Gray' }
            }

            Write-WatchStatus -Label $c.Name -Value "$($c.State) | $($c.Status)" -ValueColor $statusColor

            [PSCustomObject]@{
                PSTypeName = 'TheWatch.Container'
                Name       = $c.Name
                Service    = $c.Service
                Status     = $c.State
                Ports      = $c.Publishers | ForEach-Object { "$($_.PublishedPort)->$($_.TargetPort)" } | Join-String -Separator ', '
                Image      = $c.Image
            }
        }
    }
    finally {
        Pop-Location
    }
}

function Get-WatchLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Service,

        [int]$Tail = 100,

        [switch]$Follow
    )

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    $args = @('compose', 'logs', '--tail', $Tail.ToString())
    if ($Follow) { $args += '-f' }
    $args += $Service

    Push-Location $root
    try {
        & docker @args
    }
    finally {
        Pop-Location
    }
}

function Initialize-WatchDatabase {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    Assert-WatchPrerequisite -Tool 'docker'
    $root = Get-WatchSolutionRoot

    if (-not $PSCmdlet.ShouldProcess('SQL Server', 'Initialize 10 databases via sql-init container')) { return }

    Write-WatchHeader "Initializing TheWatch Databases"

    Push-Location $root
    try {
        & docker compose run --rm sql-init
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "All 10 databases created successfully"
        }
        else {
            Write-WatchError "Database initialization failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}
