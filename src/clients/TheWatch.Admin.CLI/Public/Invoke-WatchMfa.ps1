#
# Invoke-WatchMfa.ps1
# MFA commands: TOTP enable/confirm/disable, SMS send/confirm
#

function Enable-WatchMfa {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    if (-not $PSCmdlet.ShouldProcess('Current user', 'Enable TOTP MFA')) { return }

    $response = Invoke-WatchApi -Endpoint '/api/auth/mfa/totp/enable' -Method Post

    Write-WatchSuccess "TOTP MFA setup initiated"
    if ($response.qrCodeUri) {
        Write-WatchStatus -Label 'QR Code URI' -Value $response.qrCodeUri
    }
    if ($response.manualEntryKey) {
        Write-WatchStatus -Label 'Manual Key' -Value $response.manualEntryKey
    }
    Write-Host "  Use Confirm-WatchMfa -Code <6-digit-code> to complete setup." -ForegroundColor Gray
    $response
}

function Confirm-WatchMfa {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidatePattern('^\d{6}$')]
        [string]$Code
    )

    $body = @{ code = $Code }

    $response = Invoke-WatchApi -Endpoint '/api/auth/mfa/totp/verify' -Method Post -Body $body

    Write-WatchSuccess "TOTP MFA verified and enabled"
    if ($response.recoveryCodes) {
        Write-WatchWarning "Save these recovery codes in a secure location:"
        foreach ($rc in $response.recoveryCodes) {
            Write-Host "    $rc" -ForegroundColor Yellow
        }
    }
    $response
}

function Disable-WatchMfa {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory)]
        [ValidatePattern('^\d{6}$')]
        [string]$Code
    )

    if (-not $PSCmdlet.ShouldProcess('Current user', 'Disable TOTP MFA')) { return }

    $body = @{ code = $Code }

    $response = Invoke-WatchApi -Endpoint '/api/auth/mfa/totp/disable' -Method Post -Body $body

    Write-WatchSuccess "TOTP MFA has been disabled"
    $response
}

function Send-WatchSmsMfa {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PhoneNumber
    )

    $body = @{ phoneNumber = $PhoneNumber }

    $response = Invoke-WatchApi -Endpoint '/api/auth/mfa/sms/send' -Method Post -Body $body -NoAuth

    Write-WatchSuccess "SMS verification code sent to $PhoneNumber"
    $response
}

function Confirm-WatchSmsMfa {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PhoneNumber,

        [Parameter(Mandatory)]
        [ValidatePattern('^\d{6}$')]
        [string]$Code
    )

    $body = @{
        phoneNumber = $PhoneNumber
        code        = $Code
    }

    $response = Invoke-WatchApi -Endpoint '/api/auth/mfa/sms/verify' -Method Post -Body $body -NoAuth

    Write-WatchSuccess "SMS MFA verified for $PhoneNumber"
    $response
}
