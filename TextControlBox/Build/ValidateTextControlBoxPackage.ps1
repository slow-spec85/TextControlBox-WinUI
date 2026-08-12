[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$ExpectedVersion,

    [string]$ExpectedPackageId = "TextControlBox.WinUI.slow-spec85"
)

$ErrorActionPreference = "Stop"

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
if ([System.IO.Path]::GetExtension($resolvedPackagePath) -ne ".nupkg") {
    throw "Package path is not a .nupkg file: $resolvedPackagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -like "*.nuspec" })
    if ($nuspecEntries.Count -ne 1) {
        throw "Package must contain exactly one .nuspec file; found $($nuspecEntries.Count)."
    }

    $nuspecReader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
    try {
        [xml]$nuspec = $nuspecReader.ReadToEnd()
    }
    finally {
        $nuspecReader.Dispose()
    }

    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if ($null -eq $metadata) {
        throw "Package metadata was not found in the .nuspec file."
    }

    $packageId = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
    $packageVersion = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
    $authors = $metadata.SelectSingleNode("*[local-name()='authors']").InnerText
    $projectUrl = $metadata.SelectSingleNode("*[local-name()='projectUrl']").InnerText
    $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
    $license = $metadata.SelectSingleNode("*[local-name()='license']")
    $readme = $metadata.SelectSingleNode("*[local-name()='readme']").InnerText

    if ($packageId -ne $ExpectedPackageId) {
        throw "Package ID '$packageId' does not match expected ID '$ExpectedPackageId'."
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion) -and $packageVersion -ne $ExpectedVersion) {
        throw "Package version '$packageVersion' does not match expected version '$ExpectedVersion'."
    }

    if ($authors -notlike "*Julius Kirsch*" -or $authors -notlike "*slow-spec85*") {
        throw "Package authors must retain Julius Kirsch and include slow-spec85; found '$authors'."
    }

    $expectedRepositoryUrl = "https://github.com/slow-spec85/TextControlBox-WinUI"
    if ($projectUrl -ne $expectedRepositoryUrl) {
        throw "Package project URL '$projectUrl' does not match '$expectedRepositoryUrl'."
    }

    if ($null -eq $repository -or $repository.url -ne $expectedRepositoryUrl -or $repository.type -ne "git") {
        throw "Package repository metadata must identify the public Git repository."
    }

    if ($null -eq $license -or $license.type -ne "expression" -or $license.InnerText -ne "MIT") {
        throw "Package license must be the MIT SPDX expression."
    }

    if ($readme -ne "README.md") {
        throw "Package readme must be README.md; found '$readme'."
    }

    $dependencyIds = @(
        $metadata.SelectNodes("*[local-name()='dependencies']//*[local-name()='dependency']") |
            ForEach-Object { $_.id }
    )
    if ($dependencyIds -contains "MinVer") {
        throw "MinVer must not appear as a package dependency."
    }

    $requiredEntries = @(
        "Icon1.png",
        "README.md",
        "lib/net8.0-windows10.0.19041/TextControlBox.dll",
        "lib/net8.0-windows10.0.19041/TextControlBox.pri",
        "lib/net8.0-windows10.0.19041/TextControlBox.xml",
        "lib/net8.0-windows10.0.19041/TextControlBox/Core/CoreTextControlBox.xaml"
    )

    foreach ($requiredEntry in $requiredEntries) {
        if ($null -eq $archive.GetEntry($requiredEntry)) {
            throw "Required package entry is missing: $requiredEntry"
        }
    }

    $readmeEntry = $archive.GetEntry("README.md")
    $readmeReader = [System.IO.StreamReader]::new($readmeEntry.Open())
    try {
        $packagedReadme = $readmeReader.ReadToEnd()
    }
    finally {
        $readmeReader.Dispose()
    }

    if (-not $packagedReadme.Contains("https://www.nuget.org/packages/TextControlBox.WinUI.slow-spec85")) {
        throw "Packaged README does not link to the fork package."
    }

    if (-not $packagedReadme.Contains("maintained fork of [FrozenAssassine/TextControlBox-WinUI]")) {
        throw "Packaged README does not retain upstream attribution."
    }

    Write-Host "Validated package: $packageId $packageVersion"
}
finally {
    $archive.Dispose()
}
