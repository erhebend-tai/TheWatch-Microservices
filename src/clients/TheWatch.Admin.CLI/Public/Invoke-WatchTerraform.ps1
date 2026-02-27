#
# Invoke-WatchTerraform.ps1
# Terraform commands: Init, Plan, Apply, Destroy, Output, State
#

function Initialize-WatchCloud {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev',

        [switch]$Upgrade,

        [switch]$Reconfigure
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    if (-not $PSCmdlet.ShouldProcess("$Provider/$Environment", 'terraform init')) { return }

    Write-WatchHeader "Terraform Init — $Provider ($Environment)"
    Write-WatchStatus -Label 'Working dir' -Value $tfPath

    $args = @('init')
    if ($Upgrade) { $args += '-upgrade' }
    if ($Reconfigure) { $args += '-reconfigure' }

    Push-Location $tfPath
    try {
        & terraform @args
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "Terraform initialized for $Provider/$Environment"
        }
        else {
            Write-WatchError "terraform init failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function New-WatchCloudPlan {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev',

        [string]$OutFile,

        [string[]]$Target
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    if (-not $PSCmdlet.ShouldProcess("$Provider/$Environment", 'terraform plan')) { return }

    Write-WatchHeader "Terraform Plan — $Provider ($Environment)"

    $args = @('plan')
    if ($OutFile) { $args += "-out=$OutFile" }
    foreach ($t in $Target) {
        $args += "-target=$t"
    }

    Push-Location $tfPath
    try {
        & terraform @args
        if ($LASTEXITCODE -ne 0) {
            Write-WatchError "terraform plan failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function Deploy-WatchCloud {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev',

        [string]$PlanFile,

        [switch]$AutoApprove,

        [string[]]$Target
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    if (-not $PSCmdlet.ShouldProcess("$Provider/$Environment", 'terraform apply')) { return }

    Write-WatchHeader "Terraform Apply — $Provider ($Environment)"

    $args = @('apply')
    if ($PlanFile) {
        $args += $PlanFile
    }
    else {
        if ($AutoApprove) { $args += '-auto-approve' }
        foreach ($t in $Target) {
            $args += "-target=$t"
        }
    }

    Push-Location $tfPath
    try {
        & terraform @args
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "Terraform apply completed for $Provider/$Environment"
        }
        else {
            Write-WatchError "terraform apply failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function Remove-WatchCloud {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev',

        [switch]$AutoApprove
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    # Production safeguard: require typing "DESTROY PRODUCTION"
    if ($Environment -eq 'production') {
        $confirmation = Read-Host "Type 'DESTROY PRODUCTION' to confirm destruction of $Provider production infrastructure"
        if ($confirmation -ne 'DESTROY PRODUCTION') {
            Write-WatchError "Production destruction cancelled — confirmation text did not match."
            return
        }
    }

    if (-not $PSCmdlet.ShouldProcess("$Provider/$Environment", 'terraform destroy')) { return }

    Write-WatchHeader "Terraform Destroy — $Provider ($Environment)"
    Write-WatchWarning "This will destroy all $Provider infrastructure in $Environment!"

    $args = @('destroy')
    if ($AutoApprove) { $args += '-auto-approve' }

    Push-Location $tfPath
    try {
        & terraform @args
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess "Infrastructure destroyed for $Provider/$Environment"
        }
        else {
            Write-WatchError "terraform destroy failed (exit code $LASTEXITCODE)"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-WatchCloudOutput {
    [CmdletBinding()]
    [OutputType('TheWatch.CloudOutput')]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev'
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    Write-WatchHeader "Terraform Outputs — $Provider ($Environment)"

    Push-Location $tfPath
    try {
        $outputJson = & terraform output -json 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-WatchError "terraform output failed"
            return
        }

        $outputs = $outputJson | ConvertFrom-Json

        foreach ($prop in $outputs.PSObject.Properties) {
            $isSensitive = $prop.Value.sensitive -eq $true
            $displayValue = if ($isSensitive) { '(sensitive)' } else { $prop.Value.value }

            [PSCustomObject]@{
                PSTypeName = 'TheWatch.CloudOutput'
                Name       = $prop.Name
                Value      = $displayValue
                Sensitive  = $isSensitive
                Type       = $prop.Value.type
            }
        }
    }
    finally {
        Pop-Location
    }
}

function Get-WatchCloudState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Azure', 'AWS', 'GCP')]
        [string]$Provider,

        [ValidateSet('dev', 'staging', 'production')]
        [string]$Environment = 'dev',

        [string]$Resource
    )

    Assert-WatchPrerequisite -Tool 'terraform' -MinVersion '1.5'

    $tfPath = Get-WatchTerraformPath -Provider $Provider -Environment $Environment

    Write-WatchHeader "Terraform State — $Provider ($Environment)"

    Push-Location $tfPath
    try {
        if ($Resource) {
            & terraform state show $Resource
        }
        else {
            & terraform state list
        }

        if ($LASTEXITCODE -ne 0) {
            Write-WatchError "terraform state command failed"
        }
    }
    finally {
        Pop-Location
    }
}
