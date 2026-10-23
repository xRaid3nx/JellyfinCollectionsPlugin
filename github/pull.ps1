# Pull from GitHub backup mirror (restore only when explicitly requested).
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot
git pull --ff-only
