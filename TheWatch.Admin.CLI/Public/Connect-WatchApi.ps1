#
# Connect-WatchApi.ps1
# Connection management: Connect, Disconnect, Get-WatchSession, Register
#

function Connect-WatchApi {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$BaseUrl,

        [Parameter(Mandatory)]
        [string]$Email,

        [Parameter(Mandatory)]
        [securestring]$Password,

        [string]$MfaCode
    )

    # Normalize base URL (remove trailing slash)
    $BaseUrl = $BaseUrl.TrimEnd('/')

    # Convert SecureString to plain text for the API call
    $cred = [System.Net.NetworkCredential]::new('', $Password)
    $plainPassword = $cred.Password

    $body = @{
        email    = $Email
        password = $plainPassword
    }

    if ($MfaCode) {
        $body.mfaCode = $MfaCode
    }

    Write-Verbose "Connecting to $BaseUrl..."

    try {
        $response = Invoke-WatchApi -Endpoint '/api/auth/login' -Method Post -Body $body -NoAuth -BaseUrl $BaseUrl
    }
    catch {
        throw "Login failed: $_"
    }

    $session = @{
        BaseUrl      = $BaseUrl
        User         = $Email
        AccessToken  = $response.accessToken
        RefreshToken = $response.refreshToken
        ExpiresAt    = $response.expiresAt
    }

    Save-WatchSession -Session $session

    Write-WatchSuccess "Connected to $BaseUrl as $Email"

    # Return typed session object
    $output = [PSCustomObject]@{
        PSTypeName = 'TheWatch.Session'
        BaseUrl    = $BaseUrl
        User       = $Email
        ExpiresAt  = $response.expiresAt
        Connected  = $true
    }
    $output
}

function Disconnect-WatchApi {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    if ($PSCmdlet.ShouldProcess('Active session', 'Disconnect')) {
        $session = Read-WatchSession
        $user = if ($session) { $session.User } else { 'unknown' }

        Remove-WatchSession
        Write-WatchSuccess "Disconnected ($user)"
    }
}

function Get-WatchSession {
    [CmdletBinding()]
    [OutputType('TheWatch.Session')]
    param()

    $session = Read-WatchSession

    if ($null -eq $session) {
        Write-WatchWarning "No active session. Run Connect-WatchApi to authenticate."
        return
    }

    $isValid = Test-WatchSession

    [PSCustomObject]@{
        PSTypeName = 'TheWatch.Session'
        BaseUrl    = $session.BaseUrl
        User       = $session.User
        ExpiresAt  = $session.ExpiresAt
        Connected  = $isValid
    }
}

function Register-WatchUser {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$BaseUrl,

        [Parameter(Mandatory)]
        [string]$Email,

        [Parameter(Mandatory)]
        [securestring]$Password,

        [string]$DisplayName,

        [string]$PhoneNumber
    )

    if (-not $PSCmdlet.ShouldProcess($Email, 'Register new user')) { return }

    $BaseUrl = $BaseUrl.TrimEnd('/')

    $cred = [System.Net.NetworkCredential]::new('', $Password)

    $body = @{
        email    = $Email
        password = $cred.Password
    }

    if ($DisplayName) { $body.displayName = $DisplayName }
    if ($PhoneNumber) { $body.phoneNumber = $PhoneNumber }

    $response = Invoke-WatchApi -Endpoint '/api/auth/register' -Method Post -Body $body -NoAuth -BaseUrl $BaseUrl

    Write-WatchSuccess "User '$Email' registered successfully"
    $response
}
