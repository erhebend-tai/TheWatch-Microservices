#
# Invoke-WatchSecurityAudit.ps1
# Host & Network security audit: OS hardening, open ports, firewall, TLS,
# running processes, disk encryption, dependency vulnerabilities
#

function Test-WatchHostSecurity {
    [CmdletBinding()]
    [OutputType('TheWatch.SecurityCheck')]
    param(
        [switch]$IncludeDependencies,

        [switch]$Verbose_
    )

    Write-WatchHeader "TheWatch Host Security Audit"
    Write-Host "  Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC' -AsUTC)" -ForegroundColor Gray

    $checks = @()

    # ═══════════════════════════════════════════════════════════════
    # 1. Operating System
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Operating System"

    if ($IsWindows -or $env:OS -match 'Windows') {
        # Windows checks
        $os = Get-CimInstance -ClassName Win32_OperatingSystem
        $osName = $os.Caption
        $osBuild = $os.BuildNumber
        Write-WatchStatus -Label 'OS' -Value "$osName (Build $osBuild)" -ValueColor Cyan

        # Windows Update status
        try {
            $hotfixes = Get-HotFix | Sort-Object InstalledOn -Descending -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($hotfixes) {
                $daysSinceUpdate = ((Get-Date) - $hotfixes.InstalledOn).Days
                if ($daysSinceUpdate -le 30) {
                    Write-WatchSuccess "Last patch: $($hotfixes.HotFixID) ($daysSinceUpdate days ago)"
                    $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'patch-currency'; Status = 'Pass'; Detail = "$daysSinceUpdate days since last patch" }
                }
                else {
                    Write-WatchWarning "Last patch was $daysSinceUpdate days ago ($($hotfixes.HotFixID))"
                    $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'patch-currency'; Status = 'Warn'; Detail = "$daysSinceUpdate days since last patch" }
                }
            }
        }
        catch {
            Write-WatchWarning "Could not determine patch status"
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'patch-currency'; Status = 'Unknown'; Detail = 'Access denied or WMI error' }
        }

        # UAC status
        try {
            $uac = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -ErrorAction SilentlyContinue
            if ($uac.EnableLUA -eq 1) {
                Write-WatchSuccess "UAC enabled"
                $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'uac-enabled'; Status = 'Pass'; Detail = 'User Account Control active' }
            }
            else {
                Write-WatchError "UAC disabled — security risk"
                $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'uac-enabled'; Status = 'Fail'; Detail = 'UAC is disabled' }
            }
        }
        catch {
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'uac-enabled'; Status = 'Unknown'; Detail = 'Registry read failed' }
        }

        # BitLocker / disk encryption
        try {
            $bitlocker = Get-BitLockerVolume -MountPoint 'C:' -ErrorAction SilentlyContinue
            if ($bitlocker -and $bitlocker.ProtectionStatus -eq 'On') {
                Write-WatchSuccess "BitLocker enabled on C:"
                $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'disk-encryption'; Status = 'Pass'; Detail = "BitLocker on, encryption: $($bitlocker.EncryptionPercentage)%" }
            }
            else {
                Write-WatchWarning "BitLocker not active on C: — data at rest unencrypted"
                $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'disk-encryption'; Status = 'Warn'; Detail = 'BitLocker off or not available' }
            }
        }
        catch {
            Write-WatchStatus -Label 'Encryption' -Value 'Could not query BitLocker (requires elevation)' -ValueColor Yellow
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'disk-encryption'; Status = 'Unknown'; Detail = 'Requires admin privileges' }
        }

        # Windows Defender status
        try {
            $defender = Get-MpComputerStatus -ErrorAction SilentlyContinue
            if ($defender) {
                if ($defender.RealTimeProtectionEnabled) {
                    Write-WatchSuccess "Windows Defender real-time protection enabled"
                    $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'antivirus'; Status = 'Pass'; Detail = "Defender active, sigs: $($defender.AntivirusSignatureLastUpdated)" }
                }
                else {
                    Write-WatchError "Windows Defender real-time protection DISABLED"
                    $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'antivirus'; Status = 'Fail'; Detail = 'Real-time protection off' }
                }
            }
        }
        catch {
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'antivirus'; Status = 'Unknown'; Detail = 'Cannot query Defender' }
        }
    }
    else {
        # Linux/macOS checks
        $osInfo = & uname -a 2>&1
        Write-WatchStatus -Label 'OS' -Value $osInfo -ValueColor Cyan

        # Check if running as root
        $whoami = & whoami 2>&1
        if ($whoami -eq 'root') {
            Write-WatchWarning "Running as root — use a non-privileged user for services"
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'non-root'; Status = 'Warn'; Detail = 'Running as root' }
        }
        else {
            Write-WatchSuccess "Running as non-root user: $whoami"
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'non-root'; Status = 'Pass'; Detail = "User: $whoami" }
        }

        # Check unattended upgrades (Debian/Ubuntu)
        if (Test-Path '/etc/apt/apt.conf.d/20auto-upgrades') {
            Write-WatchSuccess "Unattended security updates configured"
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'auto-updates'; Status = 'Pass'; Detail = 'apt auto-upgrades enabled' }
        }
        elseif (Test-Path '/usr/bin/apt') {
            Write-WatchWarning "Unattended security updates not configured"
            $checks += [PSCustomObject]@{ Category = 'OS'; Check = 'auto-updates'; Status = 'Warn'; Detail = 'apt present but auto-upgrades not configured' }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # 2. Firewall
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Firewall"

    if ($IsWindows -or $env:OS -match 'Windows') {
        try {
            $fwProfiles = Get-NetFirewallProfile -ErrorAction SilentlyContinue
            foreach ($profile in $fwProfiles) {
                if ($profile.Enabled) {
                    Write-WatchSuccess "Firewall '$($profile.Name)' profile: ENABLED"
                    $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = "fw-$($profile.Name)"; Status = 'Pass'; Detail = 'Enabled' }
                }
                else {
                    Write-WatchError "Firewall '$($profile.Name)' profile: DISABLED"
                    $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = "fw-$($profile.Name)"; Status = 'Fail'; Detail = 'Disabled' }
                }
            }
        }
        catch {
            Write-WatchWarning "Could not query firewall (requires elevation)"
            $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = 'firewall'; Status = 'Unknown'; Detail = 'Needs admin' }
        }
    }
    else {
        # Check iptables / nftables / ufw
        $ufw = Get-Command -Name 'ufw' -ErrorAction SilentlyContinue
        if ($ufw) {
            $ufwStatus = & sudo ufw status 2>&1
            if ($ufwStatus -match 'active') {
                Write-WatchSuccess "UFW firewall is active"
                $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = 'ufw'; Status = 'Pass'; Detail = 'Active' }
            }
            else {
                Write-WatchError "UFW firewall is inactive"
                $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = 'ufw'; Status = 'Fail'; Detail = 'Inactive' }
            }
        }
        else {
            # Check iptables rules count
            $iptRules = & iptables -L -n 2>/dev/null | Measure-Object
            if ($iptRules.Count -gt 10) {
                Write-WatchSuccess "iptables has $($iptRules.Count) rules configured"
                $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = 'iptables'; Status = 'Pass'; Detail = "$($iptRules.Count) rules" }
            }
            else {
                Write-WatchWarning "iptables has minimal rules — consider hardening"
                $checks += [PSCustomObject]@{ Category = 'Firewall'; Check = 'iptables'; Status = 'Warn'; Detail = "$($iptRules.Count) rules" }
            }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # 3. Open Ports / Listening Services
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Listening Ports"

    # Known TheWatch ports
    $watchPorts = @(5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5200, 1433, 5432, 9092, 6379)
    $dangerousPorts = @(21, 23, 445, 3389, 5900, 8080, 8443)  # FTP, Telnet, SMB, RDP, VNC

    if ($IsWindows -or $env:OS -match 'Windows') {
        $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
            Select-Object LocalPort, OwningProcess |
            Sort-Object LocalPort -Unique
    }
    else {
        $listeners = & ss -tlnp 2>/dev/null
    }

    if ($listeners) {
        $listeningPorts = if ($IsWindows -or $env:OS -match 'Windows') {
            $listeners | ForEach-Object { $_.LocalPort }
        }
        else {
            # Parse ss output for ports
            $listeners | Select-String -Pattern ':(\d+)\s' -AllMatches | ForEach-Object { $_.Matches.Groups[1].Value }
        }

        $unexpectedPorts = $listeningPorts | Where-Object {
            $port = [int]$_
            $port -in $dangerousPorts
        }

        if ($unexpectedPorts) {
            foreach ($p in $unexpectedPorts) {
                Write-WatchError "Dangerous port open: $p"
            }
            $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'dangerous-ports'; Status = 'Fail'; Detail = "Open: $($unexpectedPorts -join ', ')" }
        }
        else {
            Write-WatchSuccess "No dangerous ports (FTP/Telnet/SMB/RDP/VNC) exposed"
            $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'dangerous-ports'; Status = 'Pass'; Detail = 'None detected' }
        }

        $totalListening = ($listeningPorts | Measure-Object).Count
        Write-WatchStatus -Label 'Total' -Value "$totalListening ports listening" -ValueColor White
        $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'total-listeners'; Status = 'Info'; Detail = "$totalListening ports" }
    }

    # ═══════════════════════════════════════════════════════════════
    # 4. TLS / Certificate Checks
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "TLS Configuration"

    # Check .NET dev certs
    $dotnetCmd = Get-Command -Name 'dotnet' -ErrorAction SilentlyContinue
    if ($dotnetCmd) {
        $devCertCheck = & dotnet dev-certs https --check 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-WatchSuccess ".NET HTTPS dev certificate trusted"
            $checks += [PSCustomObject]@{ Category = 'TLS'; Check = 'dotnet-dev-cert'; Status = 'Pass'; Detail = 'Trusted' }
        }
        else {
            Write-WatchWarning ".NET HTTPS dev cert not trusted (run: dotnet dev-certs https --trust)"
            $checks += [PSCustomObject]@{ Category = 'TLS'; Check = 'dotnet-dev-cert'; Status = 'Warn'; Detail = 'Not trusted' }
        }
    }

    # Check for weak TLS protocols in registry (Windows)
    if ($IsWindows -or $env:OS -match 'Windows') {
        $weakProtocols = @('SSL 2.0', 'SSL 3.0', 'TLS 1.0', 'TLS 1.1')
        foreach ($proto in $weakProtocols) {
            $regPath = "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\$proto\Server"
            try {
                $enabled = Get-ItemProperty -Path $regPath -Name 'Enabled' -ErrorAction SilentlyContinue
                if ($enabled -and $enabled.Enabled -eq 1) {
                    Write-WatchError "$proto is ENABLED — should be disabled"
                    $checks += [PSCustomObject]@{ Category = 'TLS'; Check = "weak-$($proto -replace ' ','')" ; Status = 'Fail'; Detail = 'Enabled in registry' }
                }
                else {
                    # Not explicitly set means OS default (usually disabled for old protocols)
                    $checks += [PSCustomObject]@{ Category = 'TLS'; Check = "weak-$($proto -replace ' ','')" ; Status = 'Pass'; Detail = 'Disabled or OS default' }
                }
            }
            catch {
                # Registry key doesn't exist — OS default applies
                $checks += [PSCustomObject]@{ Category = 'TLS'; Check = "weak-$($proto -replace ' ','')" ; Status = 'Pass'; Detail = 'Not configured (OS default)' }
            }
        }
        Write-WatchSuccess "Weak protocol scan completed"
    }

    # ═══════════════════════════════════════════════════════════════
    # 5. Docker Security
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Container Runtime"

    $dockerCmd = Get-Command -Name 'docker' -ErrorAction SilentlyContinue
    if ($dockerCmd) {
        $dockerInfo = & docker info --format '{{json .}}' 2>&1
        if ($LASTEXITCODE -eq 0) {
            try {
                $dInfo = $dockerInfo | ConvertFrom-Json
                Write-WatchSuccess "Docker running: $($dInfo.ServerVersion)"

                # Rootless mode
                if ($dInfo.SecurityOptions -match 'rootless') {
                    Write-WatchSuccess "Docker running in rootless mode"
                    $checks += [PSCustomObject]@{ Category = 'Docker'; Check = 'rootless'; Status = 'Pass'; Detail = 'Rootless mode' }
                }
                else {
                    Write-WatchStatus -Label 'Mode' -Value 'Root mode (rootless recommended for production)' -ValueColor Yellow
                    $checks += [PSCustomObject]@{ Category = 'Docker'; Check = 'rootless'; Status = 'Info'; Detail = 'Root mode' }
                }

                # Content trust
                if ($env:DOCKER_CONTENT_TRUST -eq '1') {
                    Write-WatchSuccess "Docker Content Trust (DCT) enabled"
                    $checks += [PSCustomObject]@{ Category = 'Docker'; Check = 'content-trust'; Status = 'Pass'; Detail = 'DOCKER_CONTENT_TRUST=1' }
                }
                else {
                    Write-WatchWarning "Docker Content Trust not enabled (set DOCKER_CONTENT_TRUST=1)"
                    $checks += [PSCustomObject]@{ Category = 'Docker'; Check = 'content-trust'; Status = 'Warn'; Detail = 'Not set' }
                }
            }
            catch {
                Write-WatchWarning "Could not parse Docker info"
            }
        }
        else {
            Write-WatchWarning "Docker daemon not running"
            $checks += [PSCustomObject]@{ Category = 'Docker'; Check = 'docker-running'; Status = 'Warn'; Detail = 'Daemon not running' }
        }
    }
    else {
        Write-WatchStatus -Label 'Docker' -Value 'Not installed (OK for non-container host)' -ValueColor Gray
    }

    # ═══════════════════════════════════════════════════════════════
    # 6. .NET Dependency Vulnerabilities
    # ═══════════════════════════════════════════════════════════════
    if ($IncludeDependencies -and $dotnetCmd) {
        Write-Host ""
        Write-WatchHeader "NuGet Dependency Vulnerabilities"

        $root = Get-WatchSolutionRoot
        $auditOutput = & dotnet list "$root/TheWatch.sln" package --vulnerable 2>&1

        if ($auditOutput -match 'has the following vulnerable packages') {
            Write-WatchError "Vulnerable NuGet packages detected"
            $checks += [PSCustomObject]@{ Category = 'Dependencies'; Check = 'nuget-vulns'; Status = 'Fail'; Detail = 'Vulnerable packages found' }

            $auditOutput | Where-Object { $_ -match '>' } | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Red
            }
        }
        else {
            Write-WatchSuccess "No known NuGet vulnerabilities"
            $checks += [PSCustomObject]@{ Category = 'Dependencies'; Check = 'nuget-vulns'; Status = 'Pass'; Detail = 'Clean' }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # Summary
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Security Audit Summary"

    $passCount = ($checks | Where-Object { $_.Status -eq 'Pass' }).Count
    $warnCount = ($checks | Where-Object { $_.Status -eq 'Warn' }).Count
    $failCount = ($checks | Where-Object { $_.Status -eq 'Fail' }).Count
    $unknownCount = ($checks | Where-Object { $_.Status -eq 'Unknown' }).Count

    Write-WatchStatus -Label 'Pass'    -Value $passCount    -ValueColor Green
    Write-WatchStatus -Label 'Warn'    -Value $warnCount    -ValueColor Yellow
    Write-WatchStatus -Label 'Fail'    -Value $failCount    -ValueColor Red
    Write-WatchStatus -Label 'Unknown' -Value $unknownCount -ValueColor Gray

    if ($failCount -gt 0) {
        Write-Host ""
        Write-WatchError "CRITICAL ISSUES (must fix before tunnel exposure):"
        $checks | Where-Object { $_.Status -eq 'Fail' } | ForEach-Object {
            Write-Host "    - [$($_.Category)] $($_.Check): $($_.Detail)" -ForegroundColor Red
        }
    }

    $checks | ForEach-Object {
        $_.PSObject.TypeNames.Insert(0, 'TheWatch.SecurityCheck')
    }

    return $checks
}

function Test-WatchNetworkSecurity {
    [CmdletBinding()]
    [OutputType('TheWatch.SecurityCheck')]
    param(
        [string]$Domain = 'thewatch.app',

        [switch]$DeepScan
    )

    Write-WatchHeader "TheWatch Network Security Audit"

    $checks = @()

    # ═══════════════════════════════════════════════════════════════
    # 1. Public IP & Exposure
    # ═══════════════════════════════════════════════════════════════
    Write-WatchHeader "Public Exposure"

    try {
        $publicIp = (Invoke-RestMethod -Uri 'https://api.ipify.org?format=json' -TimeoutSec 5).ip
        Write-WatchStatus -Label 'Public IP' -Value $publicIp -ValueColor Cyan

        # Warn if common service ports are directly exposed (without tunnel)
        $exposedPorts = @()
        foreach ($port in @(5100, 5101, 5105, 5200, 1433, 5432)) {
            try {
                $test = Test-NetConnection -ComputerName $publicIp -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet 2>$null
                if ($test) {
                    $exposedPorts += $port
                }
            }
            catch {}
        }

        if ($exposedPorts.Count -gt 0) {
            Write-WatchError "Service ports directly exposed on public IP: $($exposedPorts -join ', ')"
            Write-WatchError "These should only be accessible via Cloudflare Tunnel, not directly."
            $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'direct-exposure'; Status = 'Fail'; Detail = "Exposed: $($exposedPorts -join ', ')" }
        }
        else {
            Write-WatchSuccess "No TheWatch service ports directly exposed on public IP"
            $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'direct-exposure'; Status = 'Pass'; Detail = 'Ports not reachable externally' }
        }
    }
    catch {
        Write-WatchWarning "Could not determine public IP (offline or restricted)"
        $checks += [PSCustomObject]@{ Category = 'Network'; Check = 'public-ip'; Status = 'Unknown'; Detail = 'Cannot reach ipify.org' }
    }

    # ═══════════════════════════════════════════════════════════════
    # 2. DNS Security
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "DNS Configuration"

    $subdomains = @('api', 'dashboard', 'auth', 'emergency', 'responder', 'geo')
    foreach ($sub in $subdomains) {
        $fqdn = "$sub.$Domain"
        try {
            $resolved = Resolve-DnsName -Name $fqdn -Type CNAME -ErrorAction SilentlyContinue 2>$null
            if ($resolved | Where-Object { $_.NameHost -match 'cfargotunnel\.com' }) {
                Write-WatchSuccess "$fqdn -> Cloudflare Tunnel (CNAME)"
                $checks += [PSCustomObject]@{ Category = 'DNS'; Check = "cname-$sub"; Status = 'Pass'; Detail = 'Points to cfargotunnel.com' }
            }
            elseif ($resolved) {
                Write-WatchWarning "$fqdn resolves but not to Cloudflare Tunnel"
                $checks += [PSCustomObject]@{ Category = 'DNS'; Check = "cname-$sub"; Status = 'Warn'; Detail = "Resolves to: $($resolved[0].NameHost)" }
            }
            else {
                Write-WatchStatus -Label 'DNS' -Value "$fqdn not configured yet" -ValueColor Gray
                $checks += [PSCustomObject]@{ Category = 'DNS'; Check = "cname-$sub"; Status = 'Info'; Detail = 'Not configured' }
            }
        }
        catch {
            $checks += [PSCustomObject]@{ Category = 'DNS'; Check = "cname-$sub"; Status = 'Info'; Detail = 'Cannot resolve' }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # 3. Localhost Binding Audit
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Service Binding Audit"

    if ($IsWindows -or $env:OS -match 'Windows') {
        $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue
        $watchPorts = @(5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5200)

        foreach ($port in $watchPorts) {
            $binding = $listeners | Where-Object { $_.LocalPort -eq $port }
            if ($binding) {
                $addr = $binding.LocalAddress | Select-Object -First 1
                if ($addr -eq '0.0.0.0' -or $addr -eq '::') {
                    Write-WatchWarning "Port $port bound to ALL interfaces ($addr) — bind to 127.0.0.1 for tunnel-only"
                    $checks += [PSCustomObject]@{ Category = 'Network'; Check = "bind-$port"; Status = 'Warn'; Detail = "Bound to $addr" }
                }
                else {
                    Write-WatchSuccess "Port $port bound to $addr (localhost only)"
                    $checks += [PSCustomObject]@{ Category = 'Network'; Check = "bind-$port"; Status = 'Pass'; Detail = "Bound to $addr" }
                }
            }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # 4. Cloudflare Access / Zero Trust Readiness
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Zero Trust Readiness"

    $root = Get-WatchSolutionRoot
    $ztMiddleware = Join-Path -Path $root -ChildPath 'TheWatch.Admin.RestAPI/Middleware/ZeroTrustMiddleware.cs'
    if (Test-Path -Path $ztMiddleware) {
        Write-WatchSuccess "Zero Trust middleware present in REST API"
        $checks += [PSCustomObject]@{ Category = 'ZeroTrust'; Check = 'zt-middleware'; Status = 'Pass'; Detail = $ztMiddleware }
    }
    else {
        Write-WatchWarning "Zero Trust middleware not found"
        $checks += [PSCustomObject]@{ Category = 'ZeroTrust'; Check = 'zt-middleware'; Status = 'Warn'; Detail = 'Missing' }
    }

    $cfOptions = Join-Path -Path $root -ChildPath 'TheWatch.Shared/Cloudflare/CloudflareOptions.cs'
    if (Test-Path -Path $cfOptions) {
        Write-WatchSuccess "Cloudflare integration configured in Shared library"
        $checks += [PSCustomObject]@{ Category = 'ZeroTrust'; Check = 'cf-integration'; Status = 'Pass'; Detail = 'CloudflareOptions.cs present' }
    }

    $secHeaders = Join-Path -Path $root -ChildPath 'TheWatch.Shared/Security/SecurityHeadersMiddleware.cs'
    if (Test-Path -Path $secHeaders) {
        Write-WatchSuccess "Security headers middleware configured"
        $checks += [PSCustomObject]@{ Category = 'ZeroTrust'; Check = 'security-headers'; Status = 'Pass'; Detail = 'OWASP headers middleware present' }
    }
    else {
        Write-WatchWarning "Security headers middleware missing"
        $checks += [PSCustomObject]@{ Category = 'ZeroTrust'; Check = 'security-headers'; Status = 'Warn'; Detail = 'Missing' }
    }

    # ═══════════════════════════════════════════════════════════════
    # 5. Secret Management
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Secret Management"

    # Check for secrets in appsettings that aren't placeholders
    $settingsFiles = Get-ChildItem -Path $root -Filter 'appsettings*.json' -Recurse -ErrorAction SilentlyContinue
    $leakedSecrets = @()

    foreach ($file in $settingsFiles) {
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            # Check for non-placeholder passwords/keys
            if ($content -match '"(Password|Secret|Key|ApiKey|ConnectionString)":\s*"(?!(\{|<|\$|CHANGE_ME|your-))([^"]{8,})"') {
                $leakedSecrets += $file.FullName
            }
        }
    }

    if ($leakedSecrets.Count -gt 0) {
        Write-WatchError "Potential hardcoded secrets found in:"
        foreach ($f in $leakedSecrets) {
            Write-Host "    - $f" -ForegroundColor Red
        }
        $checks += [PSCustomObject]@{ Category = 'Secrets'; Check = 'hardcoded-secrets'; Status = 'Fail'; Detail = "$($leakedSecrets.Count) file(s) with potential secrets" }
    }
    else {
        Write-WatchSuccess "No hardcoded secrets detected in appsettings"
        $checks += [PSCustomObject]@{ Category = 'Secrets'; Check = 'hardcoded-secrets'; Status = 'Pass'; Detail = 'Clean' }
    }

    # Check .gitignore for sensitive patterns
    $gitignore = Join-Path -Path $root -ChildPath '.gitignore'
    if (Test-Path -Path $gitignore) {
        $gitignoreContent = Get-Content -Path $gitignore -Raw
        $requiredPatterns = @('*.pfx', '.env', 'appsettings.*.local.json')
        $missingPatterns = @()
        foreach ($pattern in $requiredPatterns) {
            if ($gitignoreContent -notmatch [regex]::Escape($pattern)) {
                $missingPatterns += $pattern
            }
        }

        if ($missingPatterns.Count -gt 0) {
            Write-WatchWarning ".gitignore missing sensitive patterns: $($missingPatterns -join ', ')"
            $checks += [PSCustomObject]@{ Category = 'Secrets'; Check = 'gitignore'; Status = 'Warn'; Detail = "Missing: $($missingPatterns -join ', ')" }
        }
        else {
            Write-WatchSuccess ".gitignore covers sensitive file patterns"
            $checks += [PSCustomObject]@{ Category = 'Secrets'; Check = 'gitignore'; Status = 'Pass'; Detail = 'All patterns present' }
        }
    }

    # ═══════════════════════════════════════════════════════════════
    # Summary
    # ═══════════════════════════════════════════════════════════════
    Write-Host ""
    Write-WatchHeader "Network Security Summary"

    $passCount = ($checks | Where-Object { $_.Status -eq 'Pass' }).Count
    $warnCount = ($checks | Where-Object { $_.Status -eq 'Warn' }).Count
    $failCount = ($checks | Where-Object { $_.Status -eq 'Fail' }).Count

    Write-WatchStatus -Label 'Pass' -Value $passCount -ValueColor Green
    Write-WatchStatus -Label 'Warn' -Value $warnCount -ValueColor Yellow
    Write-WatchStatus -Label 'Fail' -Value $failCount -ValueColor Red

    if ($failCount -eq 0 -and $warnCount -le 2) {
        Write-Host ""
        Write-WatchSuccess "Network is READY for Cloudflare Tunnel exposure"
    }
    elseif ($failCount -gt 0) {
        Write-Host ""
        Write-WatchError "CRITICAL issues must be resolved before exposing via tunnel"
    }

    $checks | ForEach-Object {
        $_.PSObject.TypeNames.Insert(0, 'TheWatch.SecurityCheck')
    }

    return $checks
}

function Get-WatchSecurityReport {
    [CmdletBinding()]
    [OutputType('TheWatch.SecurityReport')]
    param(
        [switch]$IncludeDependencies,

        [string]$Domain = 'thewatch.app',

        [string]$OutputFile
    )

    Write-WatchHeader "TheWatch Comprehensive Security Report"
    Write-Host "  Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC' -AsUTC)" -ForegroundColor Gray
    Write-Host ""

    # Run both audits
    $hostChecks = Test-WatchHostSecurity -IncludeDependencies:$IncludeDependencies
    $networkChecks = Test-WatchNetworkSecurity -Domain $Domain

    $allChecks = @($hostChecks) + @($networkChecks)

    $report = [PSCustomObject]@{
        PSTypeName  = 'TheWatch.SecurityReport'
        Timestamp   = (Get-Date -Format 'o')
        Hostname    = $env:COMPUTERNAME
        Domain      = $Domain
        TotalChecks = $allChecks.Count
        Passed      = ($allChecks | Where-Object { $_.Status -eq 'Pass' }).Count
        Warnings    = ($allChecks | Where-Object { $_.Status -eq 'Warn' }).Count
        Failures    = ($allChecks | Where-Object { $_.Status -eq 'Fail' }).Count
        Unknown     = ($allChecks | Where-Object { $_.Status -eq 'Unknown' }).Count
        TunnelReady = (($allChecks | Where-Object { $_.Status -eq 'Fail' }).Count -eq 0)
        Checks      = $allChecks
    }

    # Overall verdict
    Write-Host ""
    Write-Host ""
    Write-WatchHeader "OVERALL VERDICT"

    if ($report.TunnelReady) {
        Write-Host "  ================================================" -ForegroundColor Green
        Write-Host "  TUNNEL READY  —  No critical failures detected   " -ForegroundColor Green
        Write-Host "  ================================================" -ForegroundColor Green
    }
    else {
        Write-Host "  ================================================" -ForegroundColor Red
        Write-Host "  NOT READY  —  $($report.Failures) critical issue(s) must be fixed " -ForegroundColor Red
        Write-Host "  ================================================" -ForegroundColor Red
    }

    Write-Host ""
    Write-WatchStatus -Label 'Score' -Value "$($report.Passed) / $($report.TotalChecks) checks passed" -ValueColor $(if ($report.TunnelReady) { 'Green' } else { 'Yellow' })

    # Optional file output
    if ($OutputFile) {
        $report | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputFile -Force
        Write-WatchSuccess "Report saved to: $OutputFile"
    }

    return $report
}
