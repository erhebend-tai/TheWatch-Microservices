#
# Get-WatchSecurity.ps1
# Security commands: AuditLog, Threats, MITRE rules, DeviceTrust
#

function Get-WatchAuditLog {
    [CmdletBinding()]
    [OutputType('TheWatch.AuditEntry')]
    param(
        [int]$Page = 1,

        [ValidateRange(1, 100)]
        [int]$PageSize = 25
    )

    $response = Invoke-WatchApi -Endpoint '/api/security/audit' -Method Get -QueryParams @{
        page     = $Page.ToString()
        pageSize = $PageSize.ToString()
    }

    foreach ($entry in $response) {
        [PSCustomObject]@{
            PSTypeName = 'TheWatch.AuditEntry'
            Timestamp  = $entry.timestamp
            Action     = $entry.action
            User       = $entry.user
            IpAddress  = $entry.ipAddress
            Result     = $entry.result
            Details    = $entry.details
        }
    }
}

function Get-WatchThreat {
    [CmdletBinding()]
    param()

    $response = Invoke-WatchApi -Endpoint '/api/security/threats' -Method Get
    $response
}

function Get-WatchMitreRule {
    [CmdletBinding()]
    param()

    $response = Invoke-WatchApi -Endpoint '/api/security/mitre/rules' -Method Get
    $response
}

function Get-WatchDeviceTrust {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$UserId
    )

    process {
        $response = Invoke-WatchApi -Endpoint "/api/security/device-trust/$UserId" -Method Get
        $response
    }
}
