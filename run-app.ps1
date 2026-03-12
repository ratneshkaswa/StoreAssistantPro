param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoBuild,
    [switch]$Wait,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AppArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "StoreAssistantPro.csproj"
$framework = "net10.0-windows"
$appPath = Join-Path $repoRoot "bin\\$Configuration\\$framework\\StoreAssistantPro.exe"
$ridAppPath = Join-Path $repoRoot "bin\\$Configuration\\$framework\\win-x64\\StoreAssistantPro.exe"

if (-not (Test-Path $projectPath)) {
    throw "Could not find project file at $projectPath"
}

$resolvedAppPath = if (Test-Path $appPath) {
    $appPath
}
elseif (Test-Path $ridAppPath) {
    $ridAppPath
}
else {
    $null
}

if (-not $NoBuild -or -not $resolvedAppPath) {
    dotnet build $projectPath -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $resolvedAppPath = if (Test-Path $appPath) {
        $appPath
    }
    elseif (Test-Path $ridAppPath) {
        $ridAppPath
    }
    else {
        throw "Build completed but StoreAssistantPro.exe was not found under bin\\$Configuration\\$framework."
    }
}

if ($Wait) {
    & $resolvedAppPath @AppArgs
    exit $LASTEXITCODE
}

if ($AppArgs.Count -gt 0) {
    Start-Process -FilePath $resolvedAppPath -WorkingDirectory $repoRoot -ArgumentList $AppArgs
}
else {
    Start-Process -FilePath $resolvedAppPath -WorkingDirectory $repoRoot
}
