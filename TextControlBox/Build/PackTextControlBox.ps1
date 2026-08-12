[CmdletBinding()]
param(
    [string]$SourceName = "TextControlBoxLocal"
)

$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param(
        [string[]]$Arguments,
        [string]$Operation
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$projectPath = Join-Path $repositoryRoot "TextControlBox\TextControlBox.csproj"
$sourceLines = @(& dotnet nuget list source --format Detailed)
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read configured NuGet sources (exit code $LASTEXITCODE)."
}

$sourceHeaderPattern = "^\s*\d+\.\s+$([Regex]::Escape($SourceName))\s+\["
$packageOutput = $null
for ($index = 0; $index -lt $sourceLines.Count - 1; $index++) {
    if ($sourceLines[$index] -match $sourceHeaderPattern) {
        $packageOutput = $sourceLines[$index + 1].Trim()
        break
    }
}

if ([string]::IsNullOrWhiteSpace($packageOutput)) {
    throw "NuGet source '$SourceName' was not found. Add it with 'dotnet nuget add source <path> --name $SourceName'."
}

if ($packageOutput -match "^[a-zA-Z][a-zA-Z0-9+.-]*://") {
    throw "NuGet source '$SourceName' is not a local directory: $packageOutput"
}

New-Item -ItemType Directory -Path $packageOutput -Force | Out-Null

Push-Location $repositoryRoot
try {
    Invoke-DotNet -Operation "Release x64 build" -Arguments @(
        "build",
        $projectPath,
        "--configuration", "Release",
        "-p:Platform=x64"
    )
    Invoke-DotNet -Operation "NuGet package creation" -Arguments @(
        "pack",
        $projectPath,
        "--configuration", "Release",
        "-p:Platform=x64",
        "--no-build",
        "--output", $packageOutput
    )
}
finally {
    Pop-Location
}

Write-Host "Package created in: $packageOutput"
