[CmdletBinding()]
param(
    [string]$BackupFolder = "",
    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$documentsRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)

if ([string]::IsNullOrWhiteSpace($BackupFolder)) {
    $BackupFolder = Join-Path $documentsRoot "StoreAssistantPro\Backups"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $reportDirectory = Join-Path $repoRoot "artifacts\release-readiness"
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
    $OutputPath = Join-Path $reportDirectory "disaster-recovery-drill_$(Get-Date -Format 'yyyyMMdd_HHmmss').md"
}

$backups = @()
if (Test-Path $BackupFolder) {
    $backups = Get-ChildItem -Path $BackupFolder -Filter "*.bak" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 5
}

$lines = @(
    "# Disaster Recovery Drill"
    ""
    "- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "- BackupFolder: $BackupFolder"
    ""
    "## Latest backups"
    ""
)

if ($backups.Count -eq 0) {
    $lines += "- No backup files found."
}
else {
    $lines += $backups | ForEach-Object { "- $($_.Name) | $($_.LastWriteTime) | $([math]::Round($_.Length / 1MB, 2)) MB" }
}

$lines += ""
$lines += "## Manual drill steps"
$lines += ""
$lines += "1. Verify the selected backup with the in-app verify action."
$lines += "2. Export a support bundle before restore."
$lines += "3. Restore the selected backup on a non-production machine."
$lines += "4. Restart the application and validate login, dashboard, billing, reports, and backup listing."
$lines += "5. Record operator, machine, backup file, and outcome."

Set-Content -Path $OutputPath -Value $lines
Write-Host "Disaster recovery drill worksheet written to $OutputPath"
