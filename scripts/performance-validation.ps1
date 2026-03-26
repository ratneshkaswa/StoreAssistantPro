[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [bool]$WriteReport = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$reportDirectory = Join-Path $repoRoot "artifacts\release-readiness"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $reportDirectory "performance-validation_$timestamp.md"

if ($WriteReport) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
}

Push-Location $repoRoot
try {
    $start = Get-Date
    dotnet test .\StoreAssistantPro.Tests\StoreAssistantPro.Tests.csproj -c $Configuration --no-restore --filter "FullyQualifiedName~ReportsServiceLoadTests|FullyQualifiedName~BillingServiceLoadTests"
    $end = Get-Date
}
finally {
    Pop-Location
}

$durationSeconds = [math]::Round(($end - $start).TotalSeconds, 1)

if ($WriteReport) {
    $lines = @(
        "# Seeded Performance Validation"
        ""
        "- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "- Configuration: $Configuration"
        "- DurationSeconds: $durationSeconds"
        "- TestFilter: ReportsServiceLoadTests | BillingServiceLoadTests"
    )

    Set-Content -Path $reportPath -Value $lines
    Write-Host "Performance validation report written to $reportPath"
}
