# ComputerDoctor Agent - Automated Packaging & Release Publisher Script
# Target Framework: .NET 10.0 (win-x64)

param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "$PSScriptRoot\..\dist"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " ComputerDoctor Agent Packaging and Publisher" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$DistPath = Resolve-Path -Path $OutputDir -ErrorAction SilentlyContinue

if (-not $DistPath) {
    New-Item -ItemType Directory -Path "$ProjectRoot\dist" | Out-Null
    $DistPath = "$ProjectRoot\dist"
}

Write-Host "--> Cleaning output directory: $DistPath" -ForegroundColor Yellow
Remove-Item -Path "$DistPath\*" -Recurse -Force -ErrorAction SilentlyContinue

# 1. Publish TrayApp (WinForms Desktop Host)
Write-Host "`n[1/2] Publishing Agent.TrayApp (Desktop Tray Host)..." -ForegroundColor Green
dotnet publish "$ProjectRoot\Agent.TrayApp\Agent.TrayApp.csproj" -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "$DistPath\TrayApp"

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED to publish Agent.TrayApp!" -ForegroundColor Red
    exit 1
}

# 2. Publish Service (Background Windows Service)
Write-Host "`n[2/2] Publishing Agent.Service (Windows Background Service)..." -ForegroundColor Green
dotnet publish "$ProjectRoot\Agent.Service\Agent.Service.csproj" -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "$DistPath\Service"

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED to publish Agent.Service!" -ForegroundColor Red
    exit 1
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " SUCCESS! Standalone binaries published to: $DistPath" -ForegroundColor Green
Write-Host " TrayApp: $DistPath\TrayApp\Agent.TrayApp.exe" -ForegroundColor Gray
Write-Host " Service: $DistPath\Service\Agent.Service.exe" -ForegroundColor Gray
Write-Host "==========================================================" -ForegroundColor Cyan
