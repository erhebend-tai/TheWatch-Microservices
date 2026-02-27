#
# Invoke-WatchAspire.ps1
# Aspire AppHost management: Start, Stop
#

function Start-WatchAspire {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [switch]$NoBuild
    )

    Assert-WatchPrerequisite -Tool 'dotnet' -MinVersion '10.0'

    $root = Get-WatchSolutionRoot
    $appHostDir = Join-Path -Path $root -ChildPath 'TheWatch.Aspire.AppHost'

    if (-not (Test-Path -Path $appHostDir)) {
        throw "Aspire AppHost directory not found: $appHostDir"
    }

    if (-not $PSCmdlet.ShouldProcess('TheWatch.Aspire.AppHost', 'Start Aspire orchestration')) { return }

    # Check for existing PID
    $pidFile = Join-Path -Path $HOME -ChildPath '.thewatch/aspire.pid'
    if (Test-Path -Path $pidFile) {
        $existingPid = Get-Content -Path $pidFile -Raw
        $existingProcess = Get-Process -Id $existingPid -ErrorAction SilentlyContinue
        if ($existingProcess) {
            Write-WatchWarning "Aspire is already running (PID $existingPid). Stop it first with Stop-WatchAspire."
            return
        }
    }

    Write-WatchHeader "Starting Aspire AppHost"

    $args = @('run')
    if ($NoBuild) { $args += '--no-build' }
    $args += '--project'
    $args += $appHostDir

    $process = Start-Process -FilePath 'dotnet' -ArgumentList $args `
        -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $HOME '.thewatch/aspire.log')

    # Save PID
    $pidDir = Split-Path -Path $pidFile -Parent
    if (-not (Test-Path -Path $pidDir)) {
        New-Item -Path $pidDir -ItemType Directory -Force | Out-Null
    }
    $process.Id | Set-Content -Path $pidFile -Force

    Write-WatchSuccess "Aspire started (PID $($process.Id))"
    Write-WatchStatus -Label 'PID file' -Value $pidFile
    Write-WatchStatus -Label 'Log file' -Value (Join-Path $HOME '.thewatch/aspire.log')
    Write-WatchStatus -Label 'Dashboard' -Value 'https://localhost:17202' -ValueColor Cyan
}

function Stop-WatchAspire {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    $pidFile = Join-Path -Path $HOME -ChildPath '.thewatch/aspire.pid'

    if (-not (Test-Path -Path $pidFile)) {
        Write-WatchWarning "No Aspire PID file found. Aspire may not be running."
        return
    }

    $pid = Get-Content -Path $pidFile -Raw
    $process = Get-Process -Id $pid -ErrorAction SilentlyContinue

    if (-not $process) {
        Write-WatchWarning "Aspire process (PID $pid) is not running. Cleaning up PID file."
        Remove-Item -Path $pidFile -Force
        return
    }

    if (-not $PSCmdlet.ShouldProcess("Aspire (PID $pid)", 'Stop process')) { return }

    Write-WatchHeader "Stopping Aspire"

    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $pidFile -Force -ErrorAction SilentlyContinue

    Write-WatchSuccess "Aspire stopped (PID $pid)"
}
