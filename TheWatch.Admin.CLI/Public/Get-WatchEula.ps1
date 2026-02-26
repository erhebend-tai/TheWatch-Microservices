#
# Get-WatchEula.ps1
# EULA and Onboarding commands
#

function Get-WatchEula {
    [CmdletBinding()]
    param()

    $response = Invoke-WatchApi -Endpoint '/api/eula/current' -Method Get -NoAuth
    $response
}

function Approve-WatchEula {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    if (-not $PSCmdlet.ShouldProcess('EULA', 'Accept current EULA')) { return }

    $response = Invoke-WatchApi -Endpoint '/api/eula/accept' -Method Post

    Write-WatchSuccess "EULA accepted"
    $response
}

function Get-WatchEulaStatus {
    [CmdletBinding()]
    param()

    $response = Invoke-WatchApi -Endpoint '/api/eula/status' -Method Get
    $response
}

function Get-WatchOnboarding {
    [CmdletBinding()]
    param()

    $response = Invoke-WatchApi -Endpoint '/api/onboarding/progress' -Method Get
    $response
}

function Complete-WatchOnboardingStep {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Step
    )

    if (-not $PSCmdlet.ShouldProcess("Step '$Step'", 'Mark onboarding step complete')) { return }

    $response = Invoke-WatchApi -Endpoint '/api/onboarding/complete-step' -Method Post -QueryParams @{ step = $Step }

    Write-WatchSuccess "Onboarding step '$Step' completed"
    $response
}

function Reset-WatchOnboarding {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param()

    if (-not $PSCmdlet.ShouldProcess('Onboarding progress', 'Reset all onboarding steps')) { return }

    $response = Invoke-WatchApi -Endpoint '/api/onboarding/reset' -Method Post

    Write-WatchSuccess "Onboarding progress reset"
    $response
}
