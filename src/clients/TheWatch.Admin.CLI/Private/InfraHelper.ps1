#
# InfraHelper.ps1
# Infrastructure helper functions: solution root, prerequisites, service registry,
# Terraform path resolution, color output, and tab completion.
#

# ---------------------------------------------------------------------------
# Service Registry — all 12 microservices with ports and metadata
# ---------------------------------------------------------------------------
$script:WatchServices = @{
    'p1-coregateway'    = @{ Port = 5101; DevPort = 5101; Name = 'Core Gateway';      Container = 'thewatch-p1';        Program = 'TheWatch.P1.CoreGateway' }
    'p2-voiceemergency' = @{ Port = 5102; DevPort = 5102; Name = 'Voice Emergency';   Container = 'thewatch-p2';        Program = 'TheWatch.P2.VoiceEmergency' }
    'p3-meshnetwork'    = @{ Port = 5103; DevPort = 5103; Name = 'Mesh Network';      Container = 'thewatch-p3';        Program = 'TheWatch.P3.MeshNetwork' }
    'p4-wearable'       = @{ Port = 5104; DevPort = 5104; Name = 'Wearable';          Container = 'thewatch-p4';        Program = 'TheWatch.P4.Wearable' }
    'p5-authsecurity'   = @{ Port = 5105; DevPort = 5105; Name = 'Auth & Security';   Container = 'thewatch-p5';        Program = 'TheWatch.P5.AuthSecurity' }
    'p6-firstresponder' = @{ Port = 5106; DevPort = 5106; Name = 'First Responder';   Container = 'thewatch-p6';        Program = 'TheWatch.P6.FirstResponder' }
    'p7-familyhealth'   = @{ Port = 5107; DevPort = 5107; Name = 'Family Health';     Container = 'thewatch-p7';        Program = 'TheWatch.P7.FamilyHealth' }
    'p8-disasterrelief' = @{ Port = 5108; DevPort = 5108; Name = 'Disaster Relief';   Container = 'thewatch-p8';        Program = 'TheWatch.P8.DisasterRelief' }
    'p9-doctorservices' = @{ Port = 5109; DevPort = 5109; Name = 'Doctor Services';   Container = 'thewatch-p9';        Program = 'TheWatch.P9.DoctorServices' }
    'p10-gamification'  = @{ Port = 5110; DevPort = 5110; Name = 'Gamification';      Container = 'thewatch-p10';       Program = 'TheWatch.P10.Gamification' }
    'geospatial'        = @{ Port = 5111; DevPort = 5111; Name = 'Geospatial';        Container = 'thewatch-geo';       Program = 'TheWatch.Geospatial' }
    'dashboard'         = @{ Port = 5100; DevPort = 5100; Name = 'Dashboard';         Container = 'thewatch-dashboard'; Program = 'TheWatch.Dashboard' }
}

# ---------------------------------------------------------------------------
# Get-WatchSolutionRoot
# Walks up from $PSScriptRoot looking for TheWatch.sln. Falls back to
# $env:THEWATCH_ROOT if set.
# ---------------------------------------------------------------------------
function Get-WatchSolutionRoot {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    # Try walking up from the module directory
    $current = $PSScriptRoot
    if (-not $current) { $current = (Get-Location).Path }

    for ($i = 0; $i -lt 10; $i++) {
        $slnPath = Join-Path -Path $current -ChildPath 'TheWatch.sln'
        if (Test-Path -Path $slnPath) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if (-not $parent -or $parent -eq $current) { break }
        $current = $parent
    }

    # Fallback to environment variable
    if ($env:THEWATCH_ROOT -and (Test-Path -Path $env:THEWATCH_ROOT)) {
        return $env:THEWATCH_ROOT
    }

    throw "Cannot find TheWatch.sln. Set `$env:THEWATCH_ROOT or run from within the solution directory."
}

# ---------------------------------------------------------------------------
# Assert-WatchPrerequisite
# Validates that a required tool exists in PATH and meets minimum version.
# ---------------------------------------------------------------------------
function Assert-WatchPrerequisite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('docker', 'terraform', 'helm', 'kubectl', 'dotnet', 'cloudflared')]
        [string]$Tool,

        [string]$MinVersion
    )

    $cmd = Get-Command -Name $Tool -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "'$Tool' is not installed or not in PATH. Please install $Tool and try again."
    }

    if (-not $MinVersion) { return }

    $versionString = $null
    try {
        switch ($Tool) {
            'docker' {
                # Check docker compose v2
                $result = & docker compose version 2>&1
                if ($result -match '(\d+\.\d+\.\d+)') {
                    $versionString = $Matches[1]
                }
            }
            'terraform' {
                $result = & terraform version -json 2>&1 | ConvertFrom-Json
                $versionString = $result.terraform_version
            }
            'helm' {
                $result = & helm version --short 2>&1
                if ($result -match 'v?(\d+\.\d+\.\d+)') {
                    $versionString = $Matches[1]
                }
            }
            'kubectl' {
                $result = & kubectl version --client -o json 2>&1 | ConvertFrom-Json
                $versionString = $result.clientVersion.gitVersion -replace '^v', ''
            }
            'dotnet' {
                $versionString = & dotnet --version 2>&1
            }
            'cloudflared' {
                $result = & cloudflared --version 2>&1
                if ($result -match '(\d+\.\d+\.\d+)') {
                    $versionString = $Matches[1]
                }
            }
        }
    }
    catch {
        Write-Warning "Could not determine $Tool version: $_"
        return
    }

    if ($versionString) {
        try {
            $current = [version]($versionString -replace '[^0-9.]', '' -replace '^\.' , '' -replace '\.$', '')
            $minimum = [version]$MinVersion
            if ($current -lt $minimum) {
                throw "$Tool version $versionString is below minimum required version $MinVersion. Please upgrade."
            }
        }
        catch [System.Management.Automation.RuntimeException] {
            Write-Verbose "Version comparison failed for $Tool ($versionString vs $MinVersion): $_"
        }
    }
}

# ---------------------------------------------------------------------------
# Get-WatchTerraformPath
# Resolves provider + environment to the correct terraform working directory.
# Azure = terraform/, AWS = terraform/aws/environments/$env/,
# GCP = terraform/gcp/environments/$env/
# ---------------------------------------------------------------------------
function Get-WatchTerraformPath {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev'
    )

    $root = Get-WatchSolutionRoot

    $tfPath = switch ($Provider) {
        'Azure' { Join-Path -Path $root -ChildPath 'terraform' }
        'AWS'   { Join-Path -Path $root -ChildPath "terraform/aws/environments/$Environment" }
        'GCP'   { Join-Path -Path $root -ChildPath "terraform/gcp/environments/$Environment" }
    }

    if (-not (Test-Path -Path $tfPath)) {
        throw "Terraform directory not found: $tfPath"
    }

    return $tfPath
}

# ---------------------------------------------------------------------------
# Color output helpers
# ---------------------------------------------------------------------------
function Write-WatchHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "  $('=' * $Message.Length)" -ForegroundColor DarkCyan
}

function Write-WatchSuccess {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-WatchWarning {
    param([string]$Message)
    Write-Host "  [WARN] $Message" -ForegroundColor Yellow
}

function Write-WatchError {
    param([string]$Message)
    Write-Host "  [ERROR] $Message" -ForegroundColor Red
}

function Write-WatchStatus {
    param(
        [string]$Label,
        [string]$Value,
        [ConsoleColor]$ValueColor = 'White'
    )
    Write-Host "  $Label`: " -ForegroundColor Gray -NoNewline
    Write-Host $Value -ForegroundColor $ValueColor
}

# ---------------------------------------------------------------------------
# Register-WatchArgumentCompleters
# Tab completion for -Service, -Provider, -Environment parameters
# ---------------------------------------------------------------------------
function Register-WatchArgumentCompleters {
    [CmdletBinding()]
    param()

    # -Service completer (all 12 services)
    $serviceCompleter = {
        param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
        $script:WatchServices.Keys | Where-Object { $_ -like "$wordToComplete*" } | Sort-Object | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $script:WatchServices[$_].Name)
        }
    }

    # -Provider completer
    $providerCompleter = {
        param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
        @('Azure', 'AWS', 'GCP') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', "$_ cloud provider")
        }
    }

    # -Environment completer
    $envCompleter = {
        param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
        @('dev', 'staging', 'production') | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', "$_ environment")
        }
    }

    # Register for infrastructure cmdlets
    $serviceCmdlets = @('Restart-WatchService', 'Get-WatchLog', 'Get-WatchServiceInfo', 'Start-WatchStack', 'Stop-WatchStack')
    foreach ($cmd in $serviceCmdlets) {
        Register-ArgumentCompleter -CommandName $cmd -ParameterName 'Service' -ScriptBlock $serviceCompleter
    }

    $providerCmdlets = @('Initialize-WatchCloud', 'New-WatchCloudPlan', 'Deploy-WatchCloud', 'Remove-WatchCloud', 'Get-WatchCloudOutput', 'Get-WatchCloudState')
    foreach ($cmd in $providerCmdlets) {
        Register-ArgumentCompleter -CommandName $cmd -ParameterName 'Provider' -ScriptBlock $providerCompleter
        Register-ArgumentCompleter -CommandName $cmd -ParameterName 'Environment' -ScriptBlock $envCompleter
    }
}
