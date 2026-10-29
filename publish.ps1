# publish.ps1 - Distribution Packaging Script
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   VirtualController - Standalone Distribution Packaging" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Locate dotnet binary with SDK
$dotnetCandidates = @(
    "$env:LOCALAPPDATA\dotnet\dotnet.exe",
    "$env:USERPROFILE\.dotnet\dotnet.exe",
    "C:\Program Files\dotnet\dotnet.exe"
)
$dotnet = $null
foreach ($c in $dotnetCandidates) {
    if (Test-Path $c) {
        $sdkList = & $c --list-sdks 2>$null
        if ($sdkList) {
            $dotnet = $c
            break
        }
    }
}
if (-not $dotnet) {
    if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
        $dotnet = "dotnet"
    } else {
        Write-Error ".NET SDK could not be found. Please install .NET 8.0 SDK."
    }
}
Write-Host "Using .NET binary: $dotnet" -ForegroundColor Cyan

# Close running instances if any
Stop-Process -Name "VirtualController" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$distDir = Join-Path $PSScriptRoot "dist"
if (Test-Path $distDir) {
    Write-Host "Cleaning previous dist directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $distDir
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Write-Host "Publishing VirtualController to /dist (win-x64)..." -ForegroundColor Yellow
& $dotnet publish "src\VirtualController.App\VirtualController.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:Version=1.5.0 `
    -p:AssemblyVersion=1.5.0.0 `
    -p:FileVersion=1.5.0.0 `
    -o $distDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
}

# Ensure profiles and config directories exist in dist
$distProfiles = Join-Path $distDir "profiles"
$distConfig = Join-Path $distDir "config"
$distLogs = Join-Path $distDir "logs"

if (-not (Test-Path $distProfiles)) { New-Item -ItemType Directory -Path $distProfiles | Out-Null }
if (-not (Test-Path $distConfig)) { New-Item -ItemType Directory -Path $distConfig | Out-Null }
if (-not (Test-Path $distLogs)) { New-Item -ItemType Directory -Path $distLogs | Out-Null }

# Copy default profile and config if present
if (Test-Path "profiles") {
    Copy-Item -Path "profiles\*" -Destination $distProfiles -Recurse -Force
}
if (Test-Path "config") {
    Copy-Item -Path "config\*" -Destination $distConfig -Recurse -Force
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] Published successfully to:" -ForegroundColor Green
Write-Host "  $(Join-Path $distDir 'VirtualController.exe')" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Green
