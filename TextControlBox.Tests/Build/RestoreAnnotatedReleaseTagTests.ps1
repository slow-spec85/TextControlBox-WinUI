$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$restoreScriptPath = Join-Path $repositoryRoot "TextControlBox\Build\RestoreAnnotatedReleaseTag.ps1"

if (-not (Test-Path -LiteralPath $restoreScriptPath -PathType Leaf)) {
    throw "Annotated tag restore script was not found: $restoreScriptPath"
}

$testRoot = Join-Path `
    ([IO.Path]::GetTempPath()) `
    ("TextControlBox-RestoreAnnotatedTag-" + [Guid]::NewGuid().ToString("N"))
$sourceRoot = Join-Path $testRoot "source"
$remoteRoot = Join-Path $testRoot "remote.git"
$checkoutRoot = Join-Path $testRoot "checkout"
$tag = "v2.0.0-preview.99"
$annotation = "Annotated release notes"

try {
    New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null

    git -C $sourceRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw "Failed to initialize the source repository." }

    git -C $sourceRoot config user.name "TextControlBox Tests"
    git -C $sourceRoot config user.email "tests@example.invalid"
    Set-Content -LiteralPath (Join-Path $sourceRoot "release.txt") -Value "release"
    git -C $sourceRoot add release.txt
    git -C $sourceRoot commit --quiet -m "Release commit"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create the release commit." }

    $releaseCommit = (git -C $sourceRoot rev-parse HEAD).Trim()
    git -C $sourceRoot tag --annotate $tag --message $annotation
    if ($LASTEXITCODE -ne 0) { throw "Failed to create the annotated release tag." }

    git init --bare --quiet $remoteRoot
    if ($LASTEXITCODE -ne 0) { throw "Failed to initialize the bare remote repository." }

    git -C $sourceRoot remote add origin $remoteRoot
    git -C $sourceRoot push --quiet origin "HEAD:refs/heads/master" "refs/tags/$tag"
    if ($LASTEXITCODE -ne 0) { throw "Failed to populate the test remote repository." }

    git clone --quiet $remoteRoot $checkoutRoot
    if ($LASTEXITCODE -ne 0) { throw "Failed to clone the test remote repository." }

    git -C $checkoutRoot update-ref "refs/tags/$tag" $releaseCommit
    if ($LASTEXITCODE -ne 0) { throw "Failed to simulate the lightweight checkout tag." }

    $localTypeBeforeRestore = (git -C $checkoutRoot cat-file -t "refs/tags/$tag").Trim()
    if ($localTypeBeforeRestore -ne "commit") {
        throw "The test did not reproduce the lightweight checkout tag."
    }

    Push-Location $checkoutRoot
    try {
        $restoredCommit = & $restoreScriptPath -Tag $tag
    }
    finally {
        Pop-Location
    }

    $localTypeAfterRestore = (git -C $checkoutRoot cat-file -t "refs/tags/$tag").Trim()
    $restoredAnnotation = (git -C $checkoutRoot for-each-ref `
        --format='%(contents:subject)' "refs/tags/$tag").Trim()

    if ($localTypeAfterRestore -ne "tag") {
        throw "The annotated tag object was not restored."
    }

    if ($restoredCommit -ne $releaseCommit) {
        throw "The restored tag resolved to '$restoredCommit' instead of '$releaseCommit'."
    }

    if ($restoredAnnotation -ne $annotation) {
        throw "The restored annotation was '$restoredAnnotation' instead of '$annotation'."
    }
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

Write-Host "Annotated release tag restore regression checks passed."
