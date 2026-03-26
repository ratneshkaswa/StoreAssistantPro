[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$RunPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$reportDirectory = Join-Path $repoRoot "artifacts\release-readiness"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $reportDirectory "release-readiness_$timestamp.md"

New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    $start = Get-Date
    & $Action | Out-Host
    $end = Get-Date

    [pscustomobject]@{
        Name = $Name
        StartedAt = $start
        FinishedAt = $end
        DurationSeconds = [math]::Round(($end - $start).TotalSeconds, 1)
        Status = "Passed"
    }
}

$steps = @()

Push-Location $repoRoot
try {
    $steps += Invoke-Step "Restore packages" {
        dotnet restore .\StoreAssistantPro.Tests\StoreAssistantPro.Tests.csproj
    }

    $steps += Invoke-Step "Release build" {
        dotnet build .\StoreAssistantPro.csproj -c $Configuration --no-restore -m:1 /nr:false /p:UseSharedCompilation=false
    }

    $steps += Invoke-Step "Workflow and correctness tests" {
        dotnet test .\StoreAssistantPro.Tests\StoreAssistantPro.Tests.csproj -c $Configuration --no-restore --filter "FullyQualifiedName~BillingCheckoutWorkflowTests|FullyQualifiedName~ReportsServiceFixtureTests|FullyQualifiedName~FileLoggerProviderTests|FullyQualifiedName~OperationalRunbooksStandardsTests"
    }

    $steps += Invoke-Step "Snapshot and supportability tests" {
        dotnet test .\StoreAssistantPro.Tests\StoreAssistantPro.Tests.csproj -c $Configuration --no-restore --filter "FullyQualifiedName~ViewSnapshotBaselineTests|FullyQualifiedName~CustomControlAutomationPeerTests|FullyQualifiedName~CustomControlAccessibilityStandardsTests"
    }

    $steps += Invoke-Step "Seeded performance validation" {
        & (Join-Path $PSScriptRoot "performance-validation.ps1") -Configuration $Configuration -WriteReport:$false
    }

    if ($RunPublish) {
        $steps += Invoke-Step "Publish release" {
            & (Join-Path $PSScriptRoot "publish-release.ps1") -Configuration $Configuration
        }
    }
}
finally {
    Pop-Location
}

$lines = @(
    "# Release Readiness Report"
    ""
    "- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "- Configuration: $Configuration"
    ""
    "| Step | Status | Duration (s) |"
    "| --- | --- | ---: |"
)

$lines += $steps | ForEach-Object {
    "| $($_.Name) | $($_.Status) | $($_.DurationSeconds) |"
}

$lines += ""
$lines += "## Next actions"
$lines += ""
$lines += "1. Review the latest support bundle export path if any step failed in a field environment."
$lines += "2. Keep the generated report with the release ticket or deployment record."

Set-Content -Path $reportPath -Value $lines
Write-Host "Release readiness report written to $reportPath"
