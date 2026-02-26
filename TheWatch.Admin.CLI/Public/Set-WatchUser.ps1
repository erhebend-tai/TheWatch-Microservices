#
# Set-WatchUser.ps1
# User modification: role assignment (admin only)
#

function Set-WatchUserRole {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$UserId,

        [Parameter(Mandatory)]
        [string]$Role
    )

    process {
        if (-not $PSCmdlet.ShouldProcess("User $UserId", "Assign role '$Role'")) { return }

        $body = @{
            userId = $UserId
            role   = $Role
        }

        $response = Invoke-WatchApi -Endpoint '/api/auth/roles/assign' -Method Post -Body $body

        Write-WatchSuccess "Role '$Role' assigned to user $UserId"
        $response
    }
}
