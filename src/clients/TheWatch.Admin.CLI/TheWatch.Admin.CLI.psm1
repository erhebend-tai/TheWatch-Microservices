#
# TheWatch.Admin.CLI.psm1
# Root module loader — dot-sources Private helpers first, then Public cmdlets.
#

$ErrorActionPreference = 'Stop'

# Dot-source all private helpers (order matters: TokenManager first, then HttpHelper, then InfraHelper)
$privatePath = Join-Path -Path $PSScriptRoot -ChildPath 'Private'
foreach ($file in @('TokenManager.ps1', 'HttpHelper.ps1', 'InfraHelper.ps1')) {
    $filePath = Join-Path -Path $privatePath -ChildPath $file
    if (Test-Path -Path $filePath) {
        . $filePath
    }
    else {
        Write-Warning "TheWatch.Admin.CLI: Missing private helper '$file'"
    }
}

# Dot-source all public cmdlet files
$publicPath = Join-Path -Path $PSScriptRoot -ChildPath 'Public'
$publicFiles = Get-ChildItem -Path $publicPath -Filter '*.ps1' -ErrorAction SilentlyContinue
foreach ($file in $publicFiles) {
    . $file.FullName
}

# Register tab-completion argument completers
if (Get-Command -Name 'Register-WatchArgumentCompleters' -ErrorAction SilentlyContinue) {
    Register-WatchArgumentCompleters
}
