<#
.SYNOPSIS
  Adds (or replaces) a version entry in manifest.json, the Jellyfin plugin repository manifest.

.EXAMPLE
  .\scripts\update-manifest.ps1 -Version 1.0.0.0 -TargetAbi 12.2.0.0 -SourceUrl https://.../batch-collections_1.0.0.0_jf12.2.0.zip -Checksum <md5>
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Version,
  [Parameter(Mandatory)] [string]$TargetAbi,
  [Parameter(Mandatory)] [string]$SourceUrl,
  [Parameter(Mandatory)] [string]$Checksum,
  [string]$Changelog = '',
  [string]$ManifestPath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $ManifestPath) { $ManifestPath = Join-Path $repoRoot 'manifest.json' }
$pluginGuid = 'd7904f73-f7e5-4b0a-ae99-a33d1afd11b1'

$parsed = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$manifest = @($parsed | ForEach-Object { $_ })
$plugin = $manifest | Where-Object { $_.guid -eq $pluginGuid } | Select-Object -First 1
if (-not $plugin) { throw "Plugin $pluginGuid not found in $ManifestPath." }

$entry = [pscustomobject][ordered]@{
  version   = $Version
  changelog = $Changelog
  targetAbi = $TargetAbi
  sourceUrl = $SourceUrl
  checksum  = $Checksum
  timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
}

$others = @(@($plugin.versions) | ForEach-Object { $_ } | Where-Object { $_ -and -not ($_.version -eq $Version -and $_.targetAbi -eq $TargetAbi) })
$sorted = [object[]](@(@($entry) + $others) | Sort-Object { [version]$_.version } -Descending)
$plugin.versions = $sorted

$json = ConvertTo-Json -InputObject @($manifest) -Depth 10
[System.IO.File]::WriteAllText($ManifestPath, $json + "`n", (New-Object System.Text.UTF8Encoding $false))
Write-Host "manifest.json: $Version (targetAbi $TargetAbi) -> $SourceUrl"
