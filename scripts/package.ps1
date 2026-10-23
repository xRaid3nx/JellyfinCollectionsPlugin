<#
.SYNOPSIS
  Builds a release zip for the Jellyfin plugin catalog and prints its MD5 checksum.

.DESCRIPTION
  Output: artifacts\batch-collections_<version>_jf<JellyfinVersion>.zip (gitignored).
  Used by the release workflow and usable locally.

.EXAMPLE
  .\scripts\package.ps1 -Version 1.0.0.0 -JellyfinVersion 12.2.0
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Version,
  [string]$JellyfinVersion = '12.2.0',
  [string]$OutputDir = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'artifacts' }
$project = Join-Path $repoRoot 'src/Jellyfin.Plugin.Collections/Jellyfin.Plugin.Collections.csproj'
$publishDir = Join-Path $OutputDir "publish-jf$JellyfinVersion"

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "Version must have four parts, e.g. 1.0.0.0 (got '$Version')." }

& dotnet publish $project -c Release -o $publishDir --nologo `
  "-p:Version=$Version" "-p:AssemblyVersion=$Version" "-p:FileVersion=$Version" "-p:JellyfinVersion=$JellyfinVersion"
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$zip = Join-Path $OutputDir "batch-collections_${Version}_jf$JellyfinVersion.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -LiteralPath (Join-Path $publishDir 'Jellyfin.Plugin.Collections.dll') -DestinationPath $zip

$md5 = (Get-FileHash -LiteralPath $zip -Algorithm MD5).Hash.ToLowerInvariant()
Write-Host "Package:  $zip"
Write-Host "MD5:      $md5"

if ($env:GITHUB_OUTPUT) {
  "zip=$zip" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
  "name=$(Split-Path $zip -Leaf)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
  "md5=$md5" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}
