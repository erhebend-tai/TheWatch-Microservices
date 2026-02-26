#
# Invoke-WatchPasswordless.ps1
# Passwordless auth: Magic Link + Passkey commands
#

function Send-WatchMagicLink {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Email
    )

    $body = @{ email = $Email }

    $response = Invoke-WatchApi -Endpoint '/api/auth/magic-link/send' -Method Post -Body $body -NoAuth

    Write-WatchSuccess "Magic link sent to $Email"
    $response
}

function Confirm-WatchMagicLink {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Token,

        [Parameter(Mandatory)]
        [string]$Email
    )

    $response = Invoke-WatchApi -Endpoint '/api/auth/magic-link/verify' -Method Get -QueryParams @{
        token = $Token
        email = $Email
    } -NoAuth

    if ($response.accessToken) {
        $session = @{
            BaseUrl      = (Read-WatchSession)?.BaseUrl ?? 'http://localhost:5105'
            User         = $Email
            AccessToken  = $response.accessToken
            RefreshToken = $response.refreshToken
            ExpiresAt    = $response.expiresAt
        }
        Save-WatchSession -Session $session
        Write-WatchSuccess "Authenticated via magic link as $Email"
    }

    $response
}

function Register-WatchPasskey {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string]$DisplayName = 'TheWatch CLI'
    )

    if (-not $PSCmdlet.ShouldProcess('Current user', 'Register a new passkey')) { return }

    # Step 1: Begin registration
    $beginResponse = Invoke-WatchApi -Endpoint '/api/auth/passkey/register/begin' -Method Post -Body @{
        displayName = $DisplayName
    }

    Write-WatchWarning "Passkey registration requires a FIDO2 authenticator."
    Write-Host "  Challenge data received. In a browser-based flow, the authenticator would be prompted." -ForegroundColor Gray
    Write-Host "  For CLI usage, complete registration via the admin portal." -ForegroundColor Gray

    $beginResponse
}

function Invoke-WatchPasskeyAuth {
    [CmdletBinding()]
    param()

    # Step 1: Begin authentication
    $beginResponse = Invoke-WatchApi -Endpoint '/api/auth/passkey/authenticate/begin' -Method Post -NoAuth

    Write-WatchWarning "Passkey authentication requires a FIDO2 authenticator."
    Write-Host "  Challenge data received. In a browser-based flow, the authenticator would be prompted." -ForegroundColor Gray
    Write-Host "  For CLI usage, authenticate via the admin portal or use Connect-WatchApi." -ForegroundColor Gray

    $beginResponse
}
