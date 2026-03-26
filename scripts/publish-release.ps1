[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishProfilePath = Join-Path $repoRoot "Properties\PublishProfiles\StoreAssistantPro.Release.win-x64.pubxml"
Push-Location $repoRoot
try {
    dotnet publish .\StoreAssistantPro.csproj -c $Configuration /p:PublishProfile="$publishProfilePath" /p:UseSharedCompilation=false
}
finally {
    Pop-Location
}
