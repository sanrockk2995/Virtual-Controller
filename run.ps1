# run.ps1 - Health Check and Launcher Script
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   VirtualController - Pre-Flight Checks & Launcher" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Check Driver
Write-Host "[1/3] Checking virtual controller backend (ViGEmBus)..." -ForegroundColor Yellow
$driverService = Get-Service -Name "ViGEmBus" -ErrorAction SilentlyContinue
$driverRegistry = Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\ViGEmBus"

if ($driverService -and $driverService.Status -eq "Running") {
    Write-Host "  -> ViGEmBus kernel driver is RUNNING." -ForegroundColor Green
} elseif ($driverRegistry) {
    Write-Host "  -> ViGEmBus is installed but may not be started. Starting service..." -ForegroundColor Yellow
    Start-Service -Name "ViGEmBus" -ErrorAction SilentlyContinue
} else {
    Write-Host "  [!] ViGEmBus driver not detected. Automatically downloading and installing..." -ForegroundColor Yellow
    $installerUrl = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe"
    $tempInstaller = Join-Path $env:TEMP "ViGEmBus_Setup.exe"

    try {
        Write-Host "  -> Downloading official ViGEmBus installer from GitHub..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri $installerUrl -OutFile $tempInstaller
        Write-Host "  -> Running installer (Please accept Windows UAC elevation prompt)..." -ForegroundColor Yellow
        Start-Process -FilePath $tempInstaller -ArgumentList "/passive" -Verb RunAs -Wait
        Start-Sleep -Seconds 1
        if (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\ViGEmBus") {
            Write-Host "  -> ViGEmBus installed and verified successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "  [X] Auto-install failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  Please install manually: $installerUrl" -ForegroundColor Yellow
    }
}

# 2. Check executable or build if needed
Write-Host "[2/3] Checking application binaries..." -ForegroundColor Yellow
$exePath = Join-Path $PSScriptRoot "dist\VirtualController.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "src\VirtualController.App\bin\Release\net8.0-windows\VirtualController.exe"
}
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "src\VirtualController.App\bin\Debug\net8.0-windows\VirtualController.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "  Executable not found. Running build.ps1 first..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "build.ps1")
    $exePath = Join-Path $PSScriptRoot "src\VirtualController.App\bin\Release\net8.0-windows\VirtualController.exe"
    if (-not (Test-Path $exePath)) {
        $exePath = Join-Path $PSScriptRoot "src\VirtualController.App\bin\Debug\net8.0-windows\VirtualController.exe"
    }
}

# 3. Launch
Write-Host "[3/3] Launching VirtualController..." -ForegroundColor Green
Write-Host "  Path: $exePath" -ForegroundColor Cyan
Start-Process -FilePath $exePath
Write-Host "VirtualController launched successfully!" -ForegroundColor Green
