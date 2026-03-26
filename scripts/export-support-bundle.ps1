[CmdletBinding()]
param(
    [string]$OutputDirectory = "",
    [string]$LogDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$documentsRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\support-bundles"
}

if ([string]::IsNullOrWhiteSpace($LogDirectory)) {
    $LogDirectory = Join-Path $documentsRoot "StoreAssistantPro\Logs"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$workingDirectory = Join-Path $OutputDirectory "support_$timestamp"
$bundlePath = Join-Path $OutputDirectory "support_$timestamp.zip"

New-Item -ItemType Directory -Force -Path $workingDirectory | Out-Null

$environmentPath = Join-Path $workingDirectory "environment.txt"
@(
    "Timestamp=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "MachineName=$env:COMPUTERNAME"
    "UserName=$env:USERNAME"
    "OSVersion=$([Environment]::OSVersion.VersionString)"
    "PowerShellVersion=$($PSVersionTable.PSVersion)"
) | Set-Content -Path $environmentPath

if (Test-Path $LogDirectory) {
    $logsTarget = Join-Path $workingDirectory "logs"
    New-Item -ItemType Directory -Force -Path $logsTarget | Out-Null
    Get-ChildItem -Path $LogDirectory -Filter "app_*.log" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 5 |
        Copy-Item -Destination $logsTarget -Force
}

$appSettingsPath = Join-Path $repoRoot "appsettings.json"
if (Test-Path $appSettingsPath) {
    Copy-Item -Path $appSettingsPath -Destination (Join-Path $workingDirectory "appsettings.json") -Force
}

$releaseReportsPath = Join-Path $repoRoot "artifacts\release-readiness"
if (Test-Path $releaseReportsPath) {
    $reportsTarget = Join-Path $workingDirectory "release-readiness"
    New-Item -ItemType Directory -Force -Path $reportsTarget | Out-Null
    Get-ChildItem -Path $releaseReportsPath -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 10 |
        Copy-Item -Destination $reportsTarget -Force
}

if (Test-Path $bundlePath) {
    Remove-Item -Path $bundlePath -Force
}

Compress-Archive -Path (Join-Path $workingDirectory "*") -DestinationPath $bundlePath
Write-Host "Support bundle written to $bundlePath"
