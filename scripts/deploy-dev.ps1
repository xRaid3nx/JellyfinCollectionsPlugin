<#
.SYNOPSIS
  Builds Batch Collections and installs it into the local Jellyfin server for testing.

.DESCRIPTION
  1. dotnet build -c Release
  2. Stops Jellyfin (Windows service if -ServiceName is given and exists; otherwise the jellyfin.exe process)
  3. Copies Jellyfin.Plugin.Collections.dll, thumb.png and meta.json to <PluginsDir>\Batch Collections_<version>\
  4. Starts Jellyfin again the same way it was running (same command line), or via the tray app.

  Jellyfin usually runs elevated, so run this from an elevated PowerShell if stopping it fails.
  If you moved Jellyfin's data directory, pass -PluginsDir (the "plugins" folder inside the data directory).

.EXAMPLE
  .\scripts\deploy-dev.ps1
.EXAMPLE
  .\scripts\deploy-dev.ps1 -ServiceName JellyfinServer
.EXAMPLE
  .\scripts\deploy-dev.ps1 -NoRestart
#>
[CmdletBinding()]
param(
  [string]$PluginsDir = 'D:\Apps\Jellyfin\Data\plugins',
  [string]$JellyfinExe = 'D:\Apps\Jellyfin\jellyfin.exe',
  [string]$TrayExe = 'D:\Apps\Jellyfin\jellyfin-windows-tray\Jellyfin.Windows.Tray.exe',
  [string]$DataDir = 'D:\Apps\Jellyfin\Data',
  [string]$ServiceName = '',
  [string]$JellyfinVersion = '12.2.0',
  [switch]$NoRestart
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'src\Jellyfin.Plugin.Collections\Jellyfin.Plugin.Collections.csproj'
$pluginGuid = 'd7904f73-f7e5-4b0a-ae99-a33d1afd11b1'
$pluginName = 'Batch Collections'

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Warn([string]$message) { Write-Host "    $message" -ForegroundColor Yellow }

# ---- Build -------------------------------------------------------------------
Write-Step "Building (Release, Jellyfin $JellyfinVersion)"
& dotnet build $project -c Release "-p:JellyfinVersion=$JellyfinVersion" --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$outDir = Join-Path $repoRoot 'src\Jellyfin.Plugin.Collections\bin\Release\net10.0'
$dll = Join-Path $outDir 'Jellyfin.Plugin.Collections.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw "Build output not found: $dll" }
$version = (Get-Item -LiteralPath $dll).VersionInfo.FileVersion
$targetAbi = "$JellyfinVersion.0"
Write-Host "    Built version $version (targetAbi $targetAbi)"

if (-not (Test-Path -LiteralPath $PluginsDir)) {
  throw "Plugins folder not found: $PluginsDir. If you changed Jellyfin's data directory, pass -PluginsDir '<data dir>\plugins'."
}

# ---- Stop Jellyfin -------------------------------------------------------------
$restart = $null
$service = $null
if ($ServiceName) {
  $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
  if (-not $service) { Write-Warn "Service '$ServiceName' not found; falling back to the jellyfin.exe process." }
}

if ($service) {
  if ($service.Status -eq 'Running') {
    Write-Step "Stopping service $ServiceName"
    Stop-Service -Name $ServiceName -Force
    $restart = @{ Kind = 'Service' }
  }
} else {
  $trayWasRunning = [bool](Get-Process -Name 'Jellyfin.Windows.Tray' -ErrorAction SilentlyContinue)
  $proc = Get-CimInstance Win32_Process -Filter "Name='jellyfin.exe'" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($proc) {
    Write-Step "Stopping jellyfin.exe (PID $($proc.ProcessId))"
    $restart = @{ Kind = 'Process'; Path = $proc.ExecutablePath; CommandLine = $proc.CommandLine; Tray = $trayWasRunning }
    try {
      Stop-Process -Id $proc.ProcessId -Force
      Wait-Process -Id $proc.ProcessId -Timeout 30 -ErrorAction SilentlyContinue
    } catch {
      throw "Could not stop Jellyfin (PID $($proc.ProcessId)): $($_.Exception.Message). Run this script from an elevated PowerShell."
    }
  } else {
    Write-Warn 'Jellyfin is not running; installing without a restart cycle.'
  }
}

# ---- Install -----------------------------------------------------------------
Get-ChildItem -LiteralPath $PluginsDir -Directory -Filter "$pluginName*" -ErrorAction SilentlyContinue | ForEach-Object {
  Write-Host "    Removing old install: $($_.Name)"
  Remove-Item -LiteralPath $_.FullName -Recurse -Force
}

$target = Join-Path $PluginsDir "${pluginName}_$version"
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath $dll -Destination $target -Force
$thumb = Join-Path $repoRoot 'docs\images\thumb.png'
Copy-Item -LiteralPath $thumb -Destination $target -Force

$meta = [ordered]@{
  category    = 'General'
  changelog   = 'Local development build.'
  description = 'Add many movies and shows to a collection at once by pasting a list of titles.'
  guid        = $pluginGuid
  name        = $pluginName
  overview    = 'Add many movies and shows to a collection at once by pasting a list of titles.'
  owner       = 'xRaid3nx'
  targetAbi   = $targetAbi
  timestamp   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.0000000Z')
  version     = $version
  status      = 'Active'
  autoUpdate  = $false
  imagePath   = Join-Path $target 'thumb.png'
  assemblies  = @()
}
$meta | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'meta.json') -Encoding UTF8
Write-Step "Installed to $target"

# ---- Restart -----------------------------------------------------------------
if ($NoRestart) {
  Write-Warn 'Skipping restart (-NoRestart). Restart Jellyfin to load the plugin.'
} elseif ($restart -and $restart.Kind -eq 'Service') {
  Write-Step "Starting service $ServiceName"
  Start-Service -Name $ServiceName
} elseif ($restart -and $restart.Kind -eq 'Process') {
  if ($restart.Tray -and (Test-Path -LiteralPath $TrayExe)) {
    Write-Step 'Starting Jellyfin via the tray app'
    if (-not (Get-Process -Name 'Jellyfin.Windows.Tray' -ErrorAction SilentlyContinue)) { Start-Process -FilePath $TrayExe }
    else { Write-Warn 'The tray app is still running; use its menu to start Jellyfin if it does not restart on its own.' }
  } elseif ($restart.CommandLine) {
    Write-Step 'Starting jellyfin.exe with its previous command line'
    $exe = if ($restart.Path) { $restart.Path } else { $JellyfinExe }
    $quotedExe = [regex]::Escape("`"$exe`"")
    $argLine = ($restart.CommandLine -replace "^\s*($quotedExe|$([regex]::Escape($exe)))", '').Trim()
    if ($argLine) { Start-Process -FilePath $exe -ArgumentList $argLine -WorkingDirectory (Split-Path $exe) }
    else { Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) }
  } else {
    Write-Step "Starting jellyfin.exe --datadir `"$DataDir`""
    Write-Warn 'Could not read the previous command line (it needs elevation). Using -DataDir.'
    Start-Process -FilePath $JellyfinExe -ArgumentList @('--datadir', "`"$DataDir`"") -WorkingDirectory (Split-Path $JellyfinExe)
  }
}

Write-Host ''
Write-Host 'Done. In Jellyfin: Dashboard > Plugins should list "Batch Collections" as Active,' -ForegroundColor Green
Write-Host 'and "Batch Collections" appears in the dashboard sidebar.' -ForegroundColor Green
Write-Host "Logs: $(Join-Path $DataDir 'log')  (look for lines starting with 'Batch Collections:')"
