param(
    # Build configuration where your DLL is located (bin\<Configuration>\...)
    [string]$Configuration = 'Release'
)

# --- Paths you may want to adjust ---
# Project root = folder containing this script
$projectRoot = Split-Path -Parent $PSCommandPath

# Path to compiled plugin DLL
$sourceDll = Join-Path $projectRoot "bin\$Configuration\DashboardDataProviderPlugin.dll"

# SimHub install directory
$simHubDir = 'C:\Program Files (x86)\SimHub'

# Destination for the DLL
# If your plugin actually lives in SimHub\Plugins\, change this to "$simHubDir\Plugins"
$destinationDll = Join-Path $simHubDir 'DashboardDataProviderPlugin.dll'

# SimHub executable
$simHubExe = Join-Path $simHubDir 'SimHubWPF.exe'

Write-Host "Using source DLL: $sourceDll"
Write-Host "Destination DLL:  $destinationDll"
Write-Host "SimHub EXE:        $simHubExe"
Write-Host ""

# --- Stop SimHub if running ---
Write-Host "Stopping SimHub if running..." -ForegroundColor Cyan
Get-Process | Where-Object { $_.ProcessName -like 'SimHub*' } `
    -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 2

# --- Validate DLL exists ---
if (-not (Test-Path $sourceDll)) {
    Write-Error "Source DLL not found: $sourceDll. Build the project first."
    exit 1
}

# --- Copy DLL ---
Write-Host "Copying plugin DLL to SimHub folder..." -ForegroundColor Cyan
Copy-Item -Path $sourceDll -Destination $destinationDll -Force

# --- Start SimHub ---
Write-Host "Starting SimHub..." -ForegroundColor Cyan
Start-Process -FilePath $simHubExe

Write-Host "Done. SimHub restarted with updated plugin DLL." -ForegroundColor Green