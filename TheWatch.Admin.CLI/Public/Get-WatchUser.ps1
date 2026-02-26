#
# Get-WatchUser.ps1
# User retrieval: current user (-Me) or all users (-All, admin only)
#

function Get-WatchUser {
    [CmdletBinding(DefaultParameterSetName = 'Me')]
    [OutputType('TheWatch.User')]
    param(
        [Parameter(ParameterSetName = 'Me')]
        [switch]$Me,

        [Parameter(ParameterSetName = 'All')]
        [switch]$All
    )

    if ($All) {
        $response = Invoke-WatchApi -Endpoint '/api/auth/users' -Method Get

        foreach ($user in $response) {
            [PSCustomObject]@{
                PSTypeName  = 'TheWatch.User'
                UserId      = $user.id
                Email       = $user.email
                DisplayName = $user.displayName
                Roles       = ($user.roles -join ', ')
                MfaEnabled  = $user.mfaEnabled
                CreatedAt   = $user.createdAt
                LastLogin   = $user.lastLogin
            }
        }
    }
    else {
        $response = Invoke-WatchApi -Endpoint '/api/auth/me' -Method Get

        [PSCustomObject]@{
            PSTypeName  = 'TheWatch.User'
            UserId      = $response.id
            Email       = $response.email
            DisplayName = $response.displayName
            Roles       = ($response.roles -join ', ')
            MfaEnabled  = $response.mfaEnabled
            CreatedAt   = $response.createdAt
            LastLogin   = $response.lastLogin
        }
    }
}
