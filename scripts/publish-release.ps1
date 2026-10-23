<#
.SYNOPSIS
  Publishes a release: tests, packages, creates the GitHub release, updates manifest.json, commits and pushes.

.DESCRIPTION
  Run from a clean working tree after bumping the version in Directory.Build.props / build.yaml
  and adding a CHANGELOG.md entry. manifest.json is updated locally first, so local stays the source of truth.

.EXAMPLE
  .\scripts\publish-release.ps1 -Version 1.0.0.0
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Version,
  [string]$JellyfinVersion = '12.2.0',
  [string]$Repo = 'xRaid3nx/JellyfinCollectionsPlugin',
  [string]$Notes = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# git and gh write progress to stderr; PowerShell 5.1 turns that into errors when redirected.
function Invoke-Native([string]$What, [scriptblock]$Command) {
  $ErrorActionPreference = 'Continue'
  & $Command 2>&1 | ForEach-Object { Write-Host "    $_" }
  if ($LASTEXITCODE -ne 0) { throw "$What failed (exit code $LASTEXITCODE)." }
}

Push-Location $repoRoot
try {
  if (git status --porcelain) { throw 'Working tree has uncommitted changes. Commit them first.' }
  $tag = "v$Version"
  $props = Get-Content -Raw 'Directory.Build.props'
  if ($props -notmatch "<Version>$([regex]::Escape($Version))</Version>") { throw "Directory.Build.props does not have <Version>$Version</Version>." }

  Write-Host "==> Testing"
  & dotnet test -c Release "-p:JellyfinVersion=$JellyfinVersion" --nologo
  if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

  Write-Host "==> Packaging"
  & (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -JellyfinVersion $JellyfinVersion | Out-Host
  $name = "batch-collections_${Version}_jf$JellyfinVersion.zip"
  $zip = Join-Path $repoRoot "artifacts\$name"
  $md5 = (Get-FileHash -LiteralPath $zip -Algorithm MD5).Hash.ToLowerInvariant()

  Write-Host "==> Pushing commits and creating GitHub release $tag"
  Invoke-Native 'git push' { git push origin HEAD }
  if (-not $Notes) { $Notes = "Batch Collections $Version for Jellyfin $JellyfinVersion. See CHANGELOG.md." }
  $head = git rev-parse HEAD
  Invoke-Native 'gh release create' { gh release create $tag $zip --repo $Repo --target $head --title "Batch Collections $Version" --notes $Notes }
  Invoke-Native 'git fetch tag' { git fetch origin "refs/tags/${tag}:refs/tags/$tag" --quiet }

  Write-Host "==> Updating manifest.json"
  $url = "https://github.com/$Repo/releases/download/$tag/$name"
  $abi = "$JellyfinVersion.0"
  & (Join-Path $PSScriptRoot 'update-manifest.ps1') -Version $Version -TargetAbi $abi -SourceUrl $url -Checksum $md5 `
    -Changelog $Notes
  Invoke-Native 'git commit' { git add manifest.json; git commit -m "manifest: $Version" --quiet }
  Invoke-Native 'git push' { git push origin HEAD }

  Write-Host ""
  Write-Host "Released $tag. Repository URL for Jellyfin:"
  Write-Host "  https://raw.githubusercontent.com/$Repo/main/manifest.json"
}
finally {
  Pop-Location
}
