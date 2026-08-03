# ComputerDoctor Agent - Windows Service Installer Script
param (
    [switch]$Quiet,
    [switch]$NoRestart,
    [switch]$Uninstall,
    [string]$ServiceName = "ComputerDoctorAgent",
    [string]$DisplayName = "ComputerDoctor Telemetry & Remediation Service",
    [string]$ExePath = "$PSScriptRoot\..\dist\Service\Agent.Service.exe"
)

if (-not $Quiet) {
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host " ⚙️ ComputerDoctor Windows Service Installer" -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan
}

if ($Uninstall) {
    if (-not $Quiet) { Write-Host "Stopping and removing service '$ServiceName'..." -ForegroundColor Yellow }
    sc.exe stop $ServiceName | Out-Null
    sc.exe delete $ServiceName | Out-Null
    if (-not $Quiet) { Write-Host "Service '$ServiceName' removed successfully." -ForegroundColor Green }
    exit 0
}

$ResolvedExe = [System.IO.Path]::GetFullPath($ExePath)

if (-not (Test-Path $ResolvedExe)) {
    Write-Host "ERROR: Executable not found at $ResolvedExe. Run publish.ps1 first!" -ForegroundColor Red
    exit 1
}

if (-not $Quiet) { Write-Host "Registering Windows Service '$ServiceName' -> $ResolvedExe" -ForegroundColor Cyan }

sc.exe create $ServiceName binPath= "`"$ResolvedExe`"" start= auto DisplayName= "$DisplayName" | Out-Null
sc.exe description $ServiceName "Monitors PC hardware telemetry, SMART health, and handles remote remediation commands from paired mobile devices." | Out-Null

if (-not $NoRestart) {
    if (-not $Quiet) { Write-Host "Starting service '$ServiceName'..." -ForegroundColor Green }
    sc.exe start $ServiceName | Out-Null
}

if (-not $Quiet) {
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host " SUCCESS: Windows Service registered and active!" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Cyan
}
