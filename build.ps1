# build.ps1 - Automated Build and Verification Script for VirtualController
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   VirtualController - Build & Test Pipeline" -ForegroundColor Cyan
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

Write-Host "[1/3] Restoring dependencies..." -ForegroundColor Yellow
& $dotnet restore VirtualController.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed!"
}

Write-Host "[2/3] Building solution (Release mode)..." -ForegroundColor Yellow
& $dotnet build VirtualController.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
}

Write-Host "[3/3] Running automated unit tests..." -ForegroundColor Yellow
& $dotnet test tests\VirtualController.Tests\VirtualController.Tests.csproj -c Release --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Unit tests failed!"
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] All projects built and all unit tests passed!  " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
