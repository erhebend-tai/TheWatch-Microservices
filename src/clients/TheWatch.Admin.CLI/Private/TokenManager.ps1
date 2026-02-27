#
# TokenManager.ps1
# Private helper functions for managing authentication tokens in TheWatch.Admin.CLI.
# These functions are NOT exported from the module.
#

# ---------------------------------------------------------------------------
# Script-scoped in-memory session cache (avoids disk reads on every call)
# ---------------------------------------------------------------------------
$script:WatchSession = $null

# ---------------------------------------------------------------------------
# Get-SessionPath
# Returns the platform-appropriate path to the persisted session file.
# ---------------------------------------------------------------------------
function Get-SessionPath {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    if ($IsWindows -or ($PSVersionTable.PSVersion.Major -le 5)) {
        return [System.IO.Path]::Combine($env:USERPROFILE, '.thewatch', 'session.json')
    }
    else {
        return [System.IO.Path]::Combine($HOME, '.thewatch', 'session.json')
    }
}

# ---------------------------------------------------------------------------
# Save-WatchSession
# Persists session data to disk. On Windows the token strings are encrypted
# with DPAPI (CurrentUser scope) and stored as Base64. On other platforms
# the JSON is written as plain text with restrictive file permissions.
# ---------------------------------------------------------------------------
function Save-WatchSession {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Session
    )

    # Update the in-memory cache
    $script:WatchSession = $Session

    $sessionPath = Get-SessionPath
    $sessionDir  = [System.IO.Path]::GetDirectoryName($sessionPath)

    # Ensure the .thewatch directory exists
    if (-not (Test-Path -Path $sessionDir)) {
        New-Item -Path $sessionDir -ItemType Directory -Force | Out-Null
    }

    $isWindows = $IsWindows -or ($PSVersionTable.PSVersion.Major -le 5)

    if ($isWindows) {
        # Attempt DPAPI encryption for token strings
        $dataToSave = @{
            BaseUrl    = $Session.BaseUrl
            User       = $Session.User
            ExpiresAt  = $Session.ExpiresAt
            Encrypted  = $false
        }

        try {
            Add-Type -AssemblyName System.Security -ErrorAction Stop

            $accessBytes  = [System.Text.Encoding]::UTF8.GetBytes($Session.AccessToken)
            $refreshBytes = [System.Text.Encoding]::UTF8.GetBytes($Session.RefreshToken)

            $encAccess = [System.Security.Cryptography.ProtectedData]::Protect(
                $accessBytes, $null,
                [System.Security.Cryptography.DataProtectionScope]::CurrentUser
            )
            $encRefresh = [System.Security.Cryptography.ProtectedData]::Protect(
                $refreshBytes, $null,
                [System.Security.Cryptography.DataProtectionScope]::CurrentUser
            )

            $dataToSave.AccessToken  = [Convert]::ToBase64String($encAccess)
            $dataToSave.RefreshToken = [Convert]::ToBase64String($encRefresh)
            $dataToSave.Encrypted    = $true
        }
        catch {
            Write-Warning "DPAPI encryption failed; falling back to plain-text storage. $_"
            $dataToSave.AccessToken  = $Session.AccessToken
            $dataToSave.RefreshToken = $Session.RefreshToken
            $dataToSave.Encrypted    = $false
        }

        $dataToSave | ConvertTo-Json -Depth 5 | Set-Content -Path $sessionPath -Encoding UTF8 -Force
    }
    else {
        # Non-Windows: save plain JSON then restrict permissions
        $dataToSave = @{
            BaseUrl      = $Session.BaseUrl
            User         = $Session.User
            ExpiresAt    = $Session.ExpiresAt
            AccessToken  = $Session.AccessToken
            RefreshToken = $Session.RefreshToken
            Encrypted    = $false
        }

        $dataToSave | ConvertTo-Json -Depth 5 | Set-Content -Path $sessionPath -Encoding UTF8 -Force

        # chmod 600 — owner read/write only
        try {
            chmod 600 $sessionPath 2>$null
        }
        catch {
            Write-Verbose "Could not set restrictive permissions on session file: $_"
        }
    }
}

# ---------------------------------------------------------------------------
# Read-WatchSession
# Reads and (on Windows) decrypts the persisted session. Returns $null when
# no session file exists. Updates the in-memory cache on success.
# ---------------------------------------------------------------------------
function Read-WatchSession {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param()

    # Return the in-memory cache if available
    if ($null -ne $script:WatchSession) {
        return $script:WatchSession
    }

    $sessionPath = Get-SessionPath

    if (-not (Test-Path -Path $sessionPath)) {
        return $null
    }

    try {
        $raw  = Get-Content -Path $sessionPath -Raw -Encoding UTF8
        $data = $raw | ConvertFrom-Json
    }
    catch {
        Write-Warning "Failed to read session file: $_"
        return $null
    }

    $session = @{
        BaseUrl   = $data.BaseUrl
        User      = $data.User
        ExpiresAt = $data.ExpiresAt
    }

    $isWindows = $IsWindows -or ($PSVersionTable.PSVersion.Major -le 5)

    if ($isWindows -and $data.Encrypted) {
        try {
            Add-Type -AssemblyName System.Security -ErrorAction Stop

            $encAccessBytes  = [Convert]::FromBase64String($data.AccessToken)
            $encRefreshBytes = [Convert]::FromBase64String($data.RefreshToken)

            $decAccess = [System.Security.Cryptography.ProtectedData]::Unprotect(
                $encAccessBytes, $null,
                [System.Security.Cryptography.DataProtectionScope]::CurrentUser
            )
            $decRefresh = [System.Security.Cryptography.ProtectedData]::Unprotect(
                $encRefreshBytes, $null,
                [System.Security.Cryptography.DataProtectionScope]::CurrentUser
            )

            $session.AccessToken  = [System.Text.Encoding]::UTF8.GetString($decAccess)
            $session.RefreshToken = [System.Text.Encoding]::UTF8.GetString($decRefresh)
        }
        catch {
            Write-Warning "DPAPI decryption failed: $_"
            return $null
        }
    }
    else {
        $session.AccessToken  = $data.AccessToken
        $session.RefreshToken = $data.RefreshToken
    }

    # Update in-memory cache
    $script:WatchSession = $session

    return $session
}

# ---------------------------------------------------------------------------
# Remove-WatchSession
# Deletes the session file from disk and clears the in-memory cache.
# ---------------------------------------------------------------------------
function Remove-WatchSession {
    [CmdletBinding()]
    param()

    $script:WatchSession = $null

    $sessionPath = Get-SessionPath

    if (Test-Path -Path $sessionPath) {
        Remove-Item -Path $sessionPath -Force
    }
}

# ---------------------------------------------------------------------------
# Get-WatchToken
# Returns a valid AccessToken string, refreshing it if necessary.
# This is the primary function called by HttpHelper before every API request.
# ---------------------------------------------------------------------------
function Get-WatchToken {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $session = Read-WatchSession

    if ($null -eq $session) {
        throw "No active session. Please run Connect-WatchApi to authenticate."
    }

    # Check whether the access token is expired (with a 60-second buffer)
    $expiresAt = [datetime]::Parse($session.ExpiresAt)
    $bufferTime = (Get-Date).AddSeconds(60)

    if ($bufferTime -ge $expiresAt) {
        Write-Verbose "Access token expired or expiring soon; attempting refresh..."

        $refreshBody = @{
            refreshToken = $session.RefreshToken
        } | ConvertTo-Json -Depth 3

        $refreshUrl = "$($session.BaseUrl)/api/auth/refresh"

        try {
            $response = Invoke-RestMethod -Uri $refreshUrl -Method Post `
                -ContentType 'application/json' `
                -Body $refreshBody -ErrorAction Stop

            # Update session with the refreshed tokens
            $session.AccessToken  = $response.accessToken
            $session.RefreshToken = $response.refreshToken
            $session.ExpiresAt    = $response.expiresAt

            Save-WatchSession -Session $session

            Write-Verbose "Token refreshed successfully. New expiry: $($response.expiresAt)"
        }
        catch {
            Remove-WatchSession
            throw "Session expired. Please Connect-WatchApi again."
        }
    }

    return $session.AccessToken
}

# ---------------------------------------------------------------------------
# Test-WatchSession
# Returns $true if a valid (non-expired) session exists, $false otherwise.
# ---------------------------------------------------------------------------
function Test-WatchSession {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    $session = Read-WatchSession

    if ($null -eq $session) {
        return $false
    }

    try {
        $expiresAt = [datetime]::Parse($session.ExpiresAt)
    }
    catch {
        return $false
    }

    if ((Get-Date) -ge $expiresAt) {
        return $false
    }

    return $true
}
