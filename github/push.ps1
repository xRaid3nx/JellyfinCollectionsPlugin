# Push this repo to GitHub (private backup mirror).
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$shared = 'D:\Banaat\General\Scripts\Push-RepoBackup.ps1'

if (-not (Test-Path -LiteralPath $shared)) {
  throw "Shared backup script not found: $shared"
}

& $shared -RepoPath $repoRoot @args
