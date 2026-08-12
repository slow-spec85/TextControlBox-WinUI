[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$validatorPath = Join-Path $repositoryRoot "TextControlBox\Build\ValidateTextControlBoxPackage.ps1"

if (-not (Test-Path -LiteralPath $validatorPath -PathType Leaf)) {
    throw "Package validator was not found: $validatorPath"
}

& $validatorPath -PackagePath $PackagePath

$invalidVersionWasRejected = $false
try {
    & $validatorPath -PackagePath $PackagePath -ExpectedVersion "0.0.0-invalid"
}
catch {
    if ($_.Exception.Message -notlike "*does not match expected version*") {
        throw
    }

    $invalidVersionWasRejected = $true
}

if (-not $invalidVersionWasRejected) {
    throw "The package validator accepted an incorrect expected version."
}

Write-Host "Package validator regression checks passed."
