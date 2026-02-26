#
# Invoke-WatchHelm.ps1
# Helm/Kubernetes commands: Deploy, Remove, Status, Pods
#

function Deploy-WatchHelm {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [string]$ReleaseName = 'thewatch',

        [string]$Namespace = 'thewatch',

        [string]$ChartPath,

        [string]$ValuesFile,

        [hashtable]$Set,

        [switch]$DryRun
    )

    Assert-WatchPrerequisite -Tool 'helm' -MinVersion '3.0'
    Assert-WatchPrerequisite -Tool 'kubectl'

    if (-not $ChartPath) {
        $root = Get-WatchSolutionRoot
        $ChartPath = Join-Path -Path $root -ChildPath 'helm/thewatch'
    }

    if (-not (Test-Path -Path $ChartPath)) {
        throw "Helm chart not found: $ChartPath"
    }

    if (-not $PSCmdlet.ShouldProcess("$ReleaseName in $Namespace", 'helm upgrade --install')) { return }

    Write-WatchHeader "Deploying TheWatch via Helm"

    $args = @('upgrade', '--install', $ReleaseName, $ChartPath,
              '--namespace', $Namespace, '--create-namespace')

    if ($ValuesFile) { $args += '--values'; $args += $ValuesFile }
    if ($DryRun) { $args += '--dry-run' }

    if ($Set) {
        foreach ($kv in $Set.GetEnumerator()) {
            $args += '--set'
            $args += "$($kv.Key)=$($kv.Value)"
        }
    }

    & helm @args

    if ($LASTEXITCODE -eq 0) {
        Write-WatchSuccess "Helm deploy completed ($ReleaseName)"
    }
    else {
        Write-WatchError "Helm deploy failed (exit code $LASTEXITCODE)"
    }
}

function Remove-WatchHelm {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [string]$ReleaseName = 'thewatch',

        [string]$Namespace = 'thewatch',

        [switch]$KeepHistory
    )

    Assert-WatchPrerequisite -Tool 'helm' -MinVersion '3.0'

    if (-not $PSCmdlet.ShouldProcess("$ReleaseName in $Namespace", 'helm uninstall')) { return }

    Write-WatchHeader "Removing TheWatch Helm Release"

    $args = @('uninstall', $ReleaseName, '--namespace', $Namespace)
    if ($KeepHistory) { $args += '--keep-history' }

    & helm @args

    if ($LASTEXITCODE -eq 0) {
        Write-WatchSuccess "Helm release '$ReleaseName' removed"
    }
    else {
        Write-WatchError "Helm uninstall failed (exit code $LASTEXITCODE)"
    }
}

function Get-WatchHelmStatus {
    [CmdletBinding()]
    [OutputType('TheWatch.HelmRelease')]
    param(
        [string]$ReleaseName = 'thewatch',

        [string]$Namespace = 'thewatch'
    )

    Assert-WatchPrerequisite -Tool 'helm' -MinVersion '3.0'

    Write-WatchHeader "Helm Status — $ReleaseName"

    # Get status details
    & helm status $ReleaseName --namespace $Namespace 2>&1

    # Also get release list for table output
    $listJson = & helm list --namespace $Namespace -o json 2>&1

    if ($LASTEXITCODE -eq 0 -and $listJson) {
        try {
            $releases = $listJson | ConvertFrom-Json

            foreach ($r in $releases) {
                $statusColor = switch ($r.status) {
                    'deployed'    { 'Green' }
                    'failed'      { 'Red' }
                    'pending*'    { 'Yellow' }
                    'uninstalled' { 'Gray' }
                    default       { 'White' }
                }

                Write-WatchStatus -Label $r.name -Value $r.status -ValueColor $statusColor

                [PSCustomObject]@{
                    PSTypeName = 'TheWatch.HelmRelease'
                    Name       = $r.name
                    Namespace  = $r.namespace
                    Revision   = $r.revision
                    Status     = $r.status
                    Chart      = $r.chart
                    AppVersion = $r.app_version
                }
            }
        }
        catch {
            Write-Verbose "Could not parse helm list output: $_"
        }
    }
}

function Get-WatchPod {
    [CmdletBinding()]
    [OutputType('TheWatch.Pod')]
    param(
        [string]$Namespace = 'thewatch',

        [switch]$Watch,

        [switch]$Wide
    )

    Assert-WatchPrerequisite -Tool 'kubectl'

    Write-WatchHeader "Kubernetes Pods — $Namespace"

    if ($Watch) {
        $args = @('get', 'pods', '-n', $Namespace, '--watch')
        if ($Wide) { $args += '-o'; $args += 'wide' }
        & kubectl @args
        return
    }

    $jsonOutput = & kubectl get pods -n $Namespace -o json 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-WatchError "kubectl get pods failed"
        return
    }

    try {
        $podList = $jsonOutput | ConvertFrom-Json

        foreach ($pod in $podList.items) {
            $name = $pod.metadata.name
            $phase = $pod.status.phase

            # Calculate ready containers
            $totalContainers = ($pod.spec.containers | Measure-Object).Count
            $readyContainers = ($pod.status.containerStatuses | Where-Object { $_.ready } | Measure-Object).Count
            $ready = "$readyContainers/$totalContainers"

            # Calculate restarts
            $restarts = ($pod.status.containerStatuses | Measure-Object -Property restartCount -Sum).Sum

            # Calculate age
            $creationTime = [datetime]::Parse($pod.metadata.creationTimestamp)
            $age = (Get-Date) - $creationTime
            $ageStr = if ($age.TotalDays -ge 1) { "$([int]$age.TotalDays)d" }
                      elseif ($age.TotalHours -ge 1) { "$([int]$age.TotalHours)h" }
                      else { "$([int]$age.TotalMinutes)m" }

            $statusColor = switch ($phase) {
                'Running'   { 'Green' }
                'Succeeded' { 'Green' }
                'Pending'   { 'Yellow' }
                'Failed'    { 'Red' }
                default     { 'Gray' }
            }

            Write-WatchStatus -Label $name -Value "$phase ($ready ready, $restarts restarts)" -ValueColor $statusColor

            $obj = [PSCustomObject]@{
                PSTypeName = 'TheWatch.Pod'
                Name       = $name
                Ready      = $ready
                Status     = $phase
                Restarts   = $restarts
                Age        = $ageStr
                Node       = $pod.spec.nodeName
                IP         = $pod.status.podIP
            }

            $obj
        }
    }
    catch {
        Write-WatchError "Failed to parse pod data: $_"
    }
}
