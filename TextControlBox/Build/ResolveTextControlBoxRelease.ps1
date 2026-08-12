[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Tag,

    [Parameter(Mandatory)]
    [string]$TaggedCommit,

    [Parameter(Mandatory)]
    [string]$MasterCommit
)

$ErrorActionPreference = "Stop"

$tagPattern = '^v(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'
$tagMatch = [Regex]::Match($Tag, $tagPattern)
if (-not $tagMatch.Success) {
    throw "Release tag '$Tag' must use v<major>.<minor>.<patch> with an optional SemVer prerelease suffix."
}

$prerelease = $tagMatch.Groups["prerelease"].Value
if (-not [string]::IsNullOrWhiteSpace($prerelease)) {
    foreach ($identifier in $prerelease.Split('.')) {
        if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier.StartsWith('0')) {
            throw "Numeric prerelease identifier '$identifier' in tag '$Tag' must not contain a leading zero."
        }
    }
}

$commitPattern = '^[0-9a-fA-F]{40}$'
if ($TaggedCommit -notmatch $commitPattern) {
    throw "Tagged commit '$TaggedCommit' is not a full Git commit ID."
}

if ($MasterCommit -notmatch $commitPattern) {
    throw "Public master commit '$MasterCommit' is not a full Git commit ID."
}

if (-not [string]::Equals($TaggedCommit, $MasterCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Tagged commit '$TaggedCommit' does not match public master commit '$MasterCommit'."
}

Write-Output $Tag.Substring(1)
