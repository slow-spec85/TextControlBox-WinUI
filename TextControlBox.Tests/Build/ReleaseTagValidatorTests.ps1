$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$validatorPath = Join-Path $repositoryRoot "TextControlBox\Build\ResolveTextControlBoxRelease.ps1"

if (-not (Test-Path -LiteralPath $validatorPath -PathType Leaf)) {
    throw "Release tag validator was not found: $validatorPath"
}

$releaseCommit = "1111111111111111111111111111111111111111"

$stableVersion = & $validatorPath `
    -Tag "v2.0.0" `
    -TaggedCommit $releaseCommit `
    -MasterCommit $releaseCommit
if ($stableVersion -ne "2.0.0") {
    throw "Stable release version was resolved as '$stableVersion'."
}

$previewVersion = & $validatorPath `
    -Tag "v2.1.0-preview.1" `
    -TaggedCommit $releaseCommit `
    -MasterCommit $releaseCommit
if ($previewVersion -ne "2.1.0-preview.1") {
    throw "Preview release version was resolved as '$previewVersion'."
}

$invalidTags = @(
    "2.0.0",
    "v2.0",
    "v02.0.0",
    "v2.0.0-preview.01",
    "v2.0.0+build.1"
)

foreach ($invalidTag in $invalidTags) {
    $wasRejected = $false
    try {
        & $validatorPath `
            -Tag $invalidTag `
            -TaggedCommit $releaseCommit `
            -MasterCommit $releaseCommit | Out-Null
    }
    catch {
        $wasRejected = $true
    }

    if (-not $wasRejected) {
        throw "Invalid release tag '$invalidTag' was accepted."
    }
}

$mismatchedCommitWasRejected = $false
try {
    & $validatorPath `
        -Tag "v2.0.0" `
        -TaggedCommit $releaseCommit `
        -MasterCommit "2222222222222222222222222222222222222222" | Out-Null
}
catch {
    if ($_.Exception.Message -notlike "*does not match public master commit*") {
        throw
    }

    $mismatchedCommitWasRejected = $true
}

if (-not $mismatchedCommitWasRejected) {
    throw "A release tag outside the public master tip was accepted."
}

Write-Host "Release tag validator regression checks passed."
