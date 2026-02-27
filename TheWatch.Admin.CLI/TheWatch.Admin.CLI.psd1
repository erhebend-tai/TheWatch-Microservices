@{
    RootModule        = 'TheWatch.Admin.CLI.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = 'a3f7e6d2-8b4c-4e1a-9f5d-2c8b7a6e3d10'
    Author            = 'TheWatch Team'
    CompanyName       = 'TheWatch Project'
    Copyright         = '(c) 2026 TheWatch Project. All rights reserved.'
    Description       = 'PowerShell admin and infrastructure CLI for TheWatch microservices platform. Provides API operations (auth, users, MFA, security), infrastructure management (Docker Compose, Aspire, Terraform, Helm/Kubernetes), Cloudflare Tunnel setup, and host/network security auditing.'

    PowerShellVersion = '7.0'

    FormatsToProcess  = @('TheWatch.Admin.CLI.Format.ps1xml')

    FunctionsToExport = @(
        # --- Connection (4) ---
        'Connect-WatchApi'
        'Disconnect-WatchApi'
        'Get-WatchSession'
        'Register-WatchUser'

        # --- Users (3) ---
        'Get-WatchUser'
        'Set-WatchUserRole'

        # --- MFA (5) ---
        'Enable-WatchMfa'
        'Confirm-WatchMfa'
        'Disable-WatchMfa'
        'Send-WatchSmsMfa'
        'Confirm-WatchSmsMfa'

        # --- EULA & Onboarding (6) ---
        'Get-WatchEula'
        'Approve-WatchEula'
        'Get-WatchEulaStatus'
        'Get-WatchOnboarding'
        'Complete-WatchOnboardingStep'
        'Reset-WatchOnboarding'

        # --- Security (4) ---
        'Get-WatchAuditLog'
        'Get-WatchThreat'
        'Get-WatchMitreRule'
        'Get-WatchDeviceTrust'

        # --- Health (2) ---
        'Get-WatchHealth'
        'Get-WatchServiceInfo'

        # --- Passwordless (4) ---
        'Send-WatchMagicLink'
        'Confirm-WatchMagicLink'
        'Register-WatchPasskey'
        'Invoke-WatchPasskeyAuth'

        # --- Docker (6) ---
        'Start-WatchStack'
        'Stop-WatchStack'
        'Restart-WatchService'
        'Get-WatchContainer'
        'Get-WatchLog'
        'Initialize-WatchDatabase'

        # --- Aspire (2) ---
        'Start-WatchAspire'
        'Stop-WatchAspire'

        # --- Terraform (6) ---
        'Initialize-WatchCloud'
        'New-WatchCloudPlan'
        'Deploy-WatchCloud'
        'Remove-WatchCloud'
        'Get-WatchCloudOutput'
        'Get-WatchCloudState'

        # --- Helm (4) ---
        'Deploy-WatchHelm'
        'Remove-WatchHelm'
        'Get-WatchHelmStatus'
        'Get-WatchPod'

        # --- Cloudflare Tunnel (4) ---
        'Initialize-WatchTunnel'
        'Start-WatchTunnel'
        'Stop-WatchTunnel'
        'Test-WatchTunnelHealth'

        # --- Security Audit (3) ---
        'Test-WatchHostSecurity'
        'Test-WatchNetworkSecurity'
        'Get-WatchSecurityReport'
    )

    CmdletsToExport   = @()
    VariablesToExport  = @()
    AliasesToExport    = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('TheWatch', 'Microservices', 'Admin', 'Infrastructure', 'Docker', 'Terraform', 'Helm', 'Kubernetes', 'Cloudflare', 'Security')
            ProjectUri   = 'https://github.com/TheWatch-Project'
            ReleaseNotes = '54 cmdlets: API operations, infrastructure management, Cloudflare Tunnel, and security auditing.'
        }
    }
}
