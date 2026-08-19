[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag,

    [ValidateNotNullOrEmpty()]
    [string]$Remote = "origin"
)

$ErrorActionPreference = "Stop"

$tagRef = "refs/tags/$Tag"
git check-ref-format $tagRef
if ($LASTEXITCODE -ne 0) {
    throw "Release tag '$Tag' is not a valid Git tag name."
}

git fetch --no-tags --force $Remote "+${tagRef}:${tagRef}"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to restore annotated release tag '$tagRef' from remote '$Remote'."
}

$tagObjectType = (git cat-file -t $tagRef).Trim()
if ($LASTEXITCODE -ne 0 -or $tagObjectType -ne "tag") {
    throw "Release tag '$Tag' must be an annotated tag so its annotation can be used as GitHub Release notes."
}

$taggedCommit = (git rev-parse "${tagRef}^{}").Trim()
if ($LASTEXITCODE -ne 0 -or $taggedCommit -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Failed to resolve the commit referenced by annotated release tag '$Tag'."
}

$targetObjectType = (git cat-file -t $taggedCommit).Trim()
if ($LASTEXITCODE -ne 0 -or $targetObjectType -ne "commit") {
    throw "Annotated release tag '$Tag' must reference a commit."
}

Write-Output $taggedCommit
