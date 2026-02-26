#
# HttpHelper.ps1
# Central HTTP wrapper for all API calls to TheWatch services.
#

function Invoke-WatchApi {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Endpoint,

        [ValidateSet('Get', 'Post', 'Put', 'Delete', 'Patch')]
        [string]$Method = 'Get',

        [object]$Body,

        [hashtable]$QueryParams,

        [switch]$NoAuth,

        [string]$BaseUrl
    )

    # Resolve base URL
    if (-not $BaseUrl) {
        $session = Read-WatchSession
        if ($null -eq $session) {
            throw "No active session. Run Connect-WatchApi first."
        }
        $BaseUrl = $session.BaseUrl
    }

    # Build URI with query parameters
    $uri = "$BaseUrl$Endpoint"
    if ($QueryParams -and $QueryParams.Count -gt 0) {
        $queryParts = foreach ($kv in $QueryParams.GetEnumerator()) {
            "$([Uri]::EscapeDataString($kv.Key))=$([Uri]::EscapeDataString($kv.Value))"
        }
        $uri += "?$($queryParts -join '&')"
    }

    # Build request parameters
    $params = @{
        Uri         = $uri
        Method      = $Method
        ContentType = 'application/json'
        ErrorAction = 'Stop'
    }

    # Inject Bearer token unless -NoAuth
    if (-not $NoAuth) {
        $token = Get-WatchToken
        $params['Headers'] = @{ Authorization = "Bearer $token" }
    }

    # Convert body to JSON
    if ($null -ne $Body) {
        if ($Body -is [string]) {
            $params['Body'] = $Body
        }
        else {
            $params['Body'] = $Body | ConvertTo-Json -Depth 10
        }
    }

    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        $statusCode = $null
        $errorDetail = $null

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode

            # Try to read the response body for error details
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = [System.IO.StreamReader]::new($errorStream)
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
                $errorDetail = Format-WatchApiError -ResponseBody $errorBody
            }
            catch {
                # Ignore stream read failures
            }

            # Handle 401 — attempt token refresh and retry once
            if ($statusCode -eq 401 -and -not $NoAuth) {
                Write-Verbose "Received 401 — attempting token refresh and retry..."
                try {
                    $newToken = Get-WatchToken
                    $params['Headers'] = @{ Authorization = "Bearer $newToken" }
                    $response = Invoke-RestMethod @params
                    return $response
                }
                catch {
                    throw "Authentication failed after token refresh. Please run Connect-WatchApi again."
                }
            }

            # Handle specific status codes with friendly messages
            switch ($statusCode) {
                403 { throw "Access denied (403 Forbidden). This operation requires elevated permissions.${errorDetail}" }
                404 { throw "Resource not found (404).${errorDetail}" }
                409 { throw "Conflict (409). The resource may already exist.${errorDetail}" }
                422 { throw "Validation failed (422).${errorDetail}" }
                429 { throw "Rate limited (429). Please wait and try again.${errorDetail}" }
                500 { throw "Server error (500). The service may be experiencing issues.${errorDetail}" }
                default { throw "API request failed ($statusCode).${errorDetail}" }
            }
        }

        # Connection failure (no response object)
        if ($_.Exception.Message -match 'No connection could be made|Connection refused|actively refused') {
            throw "Cannot connect to $BaseUrl. Is the service running? Start it with Start-WatchStack or Start-WatchAspire."
        }

        throw $_
    }
}

function Format-WatchApiError {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [string]$ResponseBody
    )

    if ([string]::IsNullOrWhiteSpace($ResponseBody)) {
        return ''
    }

    try {
        $parsed = $ResponseBody | ConvertFrom-Json

        # Pattern: { "error": "message" }
        if ($parsed.error) {
            return " $($parsed.error)"
        }

        # Pattern: { "title": "...", "detail": "..." } (RFC 7807 Problem Details)
        if ($parsed.title) {
            $msg = $parsed.title
            if ($parsed.detail) { $msg += " — $($parsed.detail)" }
            return " $msg"
        }

        # Pattern: { "message": "..." }
        if ($parsed.message) {
            return " $($parsed.message)"
        }

        # Pattern: { "errors": { ... } } (validation)
        if ($parsed.errors) {
            $msgs = foreach ($kv in $parsed.errors.PSObject.Properties) {
                "$($kv.Name): $($kv.Value -join ', ')"
            }
            return " $($msgs -join '; ')"
        }
    }
    catch {
        # Not JSON — return raw text (truncated)
        if ($ResponseBody.Length -gt 200) {
            return " $($ResponseBody.Substring(0, 200))..."
        }
        return " $ResponseBody"
    }

    return ''
}
