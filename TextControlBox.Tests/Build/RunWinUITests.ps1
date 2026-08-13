[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [switch]$NoBuild,

    [string]$ResultsDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$solutionPath = Join-Path $repositoryRoot "TextControlBox-WinUI.sln"
$testProjectDirectory = Join-Path $repositoryRoot "TextControlBox.Tests"
$runSettingsPath = Join-Path $testProjectDirectory "TextControlBox.Tests.runsettings"

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path $repositoryRoot "TestResults"
}
elseif (-not [System.IO.Path]::IsPathRooted($ResultsDirectory)) {
    $ResultsDirectory = Join-Path $repositoryRoot $ResultsDirectory
}

if (-not $NoBuild) {
    & dotnet build $solutionPath --configuration $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }
}

$recipeRoot = Join-Path $testProjectDirectory "bin\$Platform\$Configuration"
$recipes = @(
    Get-ChildItem -LiteralPath $recipeRoot -Filter "*.build.appxrecipe" -File -Recurse -ErrorAction SilentlyContinue
)

if ($recipes.Count -ne 1) {
    throw "Expected exactly one build.appxrecipe under '$recipeRoot'; found $($recipes.Count)."
}

$vsWherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vsWherePath -PathType Leaf)) {
    throw "vswhere.exe was not found at '$vsWherePath'. Install Visual Studio with the test tools workload."
}

$visualStudioPaths = @(
    & $vsWherePath -latest -products * -requires Microsoft.VisualStudio.Workload.Universal -property installationPath
)
if ($visualStudioPaths.Count -eq 0) {
    throw "Visual Studio with the 'WinUI application development' workload was not found. Add that workload in Visual Studio Installer."
}

$visualStudioPath = "$($visualStudioPaths[0])".Trim()

$vstestCandidates = @(
    (Join-Path $visualStudioPath "Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"),
    (Join-Path $visualStudioPath "Common7\IDE\Extensions\TestPlatform\vstest.console.exe")
)
$vstestPath = $vstestCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($vstestPath)) {
    throw "vstest.console.exe was not found under '$visualStudioPath'."
}

New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$trxFileName = "TextControlBox.Tests-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')).trx"
$vstestArguments = @(
    $recipes[0].FullName,
    "/Platform:$Platform",
    "/Framework:FrameworkUAP10",
    "/Settings:$runSettingsPath",
    "/ResultsDirectory:$ResultsDirectory",
    "/Logger:trx;LogFileName=$trxFileName"
)

Write-Host "Running packaged WinUI tests from '$($recipes[0].FullName)'."
Write-Host "Test results: '$(Join-Path $ResultsDirectory $trxFileName)'."

& $vstestPath @vstestArguments
if ($LASTEXITCODE -ne 0) {
    throw "Packaged WinUI tests failed with exit code $LASTEXITCODE."
}
