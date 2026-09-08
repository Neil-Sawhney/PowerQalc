#Requires -Version 5.1
<#
.SYNOPSIS
    Builds production Release MSIX packages (x64 + ARM64) and a Partner Center .msixbundle.
#>
param(
    [string]$Version = "1.0.3"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$project = Join-Path $root "Qalculate\Qalculate.csproj"
$qalcExe = Join-Path $root "Qalculate\qalc\qalc.exe"

Write-Host "Bundling qalc runtime..." -ForegroundColor Cyan
& (Join-Path $root "scripts\bundle-qalc.ps1")
if ($LASTEXITCODE -ne 0) { throw "bundle-qalc.ps1 failed" }

Write-Host "Generating icon assets..." -ForegroundColor Cyan
& (Join-Path $root "scripts\generate-icons.ps1")
if ($LASTEXITCODE -ne 0) { throw "generate-icons.ps1 failed" }

if (-not (Test-Path $qalcExe)) {
    throw "qalc is not bundled. Install Qalculate locally (e.g. winget install qalculate.qalculate) and re-run."
}

foreach ($platform in @("x64", "ARM64")) {
    Write-Host "`n=== Building Release $platform MSIX ===" -ForegroundColor Cyan
    & $dotnet build $project `
        -c Release `
        -p:Platform=$platform `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageDir="AppPackages\$platform\"

    if ($LASTEXITCODE -ne 0) { throw "Release MSIX build failed for $platform" }
}

Write-Host "`nMSIX packages:" -ForegroundColor Green
$msixFiles = @(Get-ChildItem (Join-Path $root "Qalculate\AppPackages") -Recurse -Filter "*.msix" |
    Sort-Object LastWriteTime -Descending)
$msixFiles | Select-Object -First 4 | ForEach-Object { Write-Host "  $($_.FullName)" }

$x64Msix = $msixFiles | Where-Object { $_.Name -match '_x64\.msix$' } | Select-Object -First 1
$arm64Msix = $msixFiles | Where-Object { $_.Name -match '_ARM64\.msix$' } | Select-Object -First 1

if ($x64Msix -and $arm64Msix) {
    $bundleVersion = if ($Version -match '^\d+\.\d+\.\d+\.\d+$') { $Version } else { "$Version.0" }
    $bundlePath = Join-Path $root "Qalculate\AppPackages\PowerQalc_$bundleVersion.msixbundle"
    $mappingFile = Join-Path $env:TEMP "powerqalc_bundle_mapping.txt"
    @"
[Files]
"$($x64Msix.FullName)" "$($x64Msix.Name)"
"$($arm64Msix.FullName)" "$($arm64Msix.Name)"
"@ | Set-Content -Path $mappingFile -Encoding ASCII

    $makeappx = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1

    if ($makeappx) {
        Write-Host "`nCreating MSIX bundle for Partner Center..." -ForegroundColor Cyan
        & $makeappx.FullName bundle /f $mappingFile /p $bundlePath /o
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  $bundlePath" -ForegroundColor Green
        }
        else {
            throw "makeappx bundle failed"
        }
    }
    else {
        throw "makeappx.exe not found. Install the Windows SDK to create the .msixbundle."
    }
}

Write-Host "`nBuild complete. Upload the .msixbundle to Partner Center." -ForegroundColor Green
