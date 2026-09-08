#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$project = Join-Path $root "Qalculate\Qalculate.csproj"

Write-Host "Bundling qalc runtime..." -ForegroundColor Cyan
& (Join-Path $root "scripts\bundle-qalc.ps1")
if ($LASTEXITCODE -ne 0) { throw "bundle-qalc.ps1 failed" }

Write-Host "Generating icon assets..." -ForegroundColor Cyan
& (Join-Path $root "scripts\generate-icons.ps1")
if ($LASTEXITCODE -ne 0) { throw "generate-icons.ps1 failed" }

Write-Host "Ensuring dev signing certificate..." -ForegroundColor Cyan
& (Join-Path $root "scripts\ensure-dev-cert.ps1")
if ($LASTEXITCODE -ne 0) { throw "ensure-dev-cert.ps1 failed" }

# Build outside OneDrive — MSBuild cannot delete locked *_Debug_Test folders under sync.
$debugAppPackages = Join-Path $env:LOCALAPPDATA "PowerQalc\AppPackages"
& (Join-Path $root "scripts\prepare-debug-build.ps1") -AppPackagesDir $debugAppPackages
if ($LASTEXITCODE -ne 0) { throw "prepare-debug-build.ps1 failed" }

Write-Host "Building Debug x64 MSIX..." -ForegroundColor Cyan
Write-Host "  Output: $debugAppPackages" -ForegroundColor DarkGray
& $dotnet build $project -c Debug -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageDir="$debugAppPackages\"
if ($LASTEXITCODE -ne 0) { throw "Debug MSIX build failed" }

& (Join-Path $root "scripts\install-debug-msix.ps1") -AppPackagesDir $debugAppPackages -Force

Write-Host ""
Write-Host "Installed. Open Command Palette and run: Reload"
Write-Host "Then search for 'PowerQalc' and try: 5 miles + 10 km, 10 mph * x = 20 mi to min, 240 * 15%, 1 ly to km"
Write-Host ""
Write-Host "This was a local test install (Debug, dev-signed)." -ForegroundColor DarkGray
Write-Host "For Store upload, run:" -ForegroundColor Yellow
Write-Host "  .\build.ps1" -ForegroundColor Yellow
Write-Host "Then upload Qalculate\AppPackages\PowerQalc_1.0.3.0.msixbundle to Partner Center."
