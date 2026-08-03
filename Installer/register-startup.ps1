# ComputerDoctor Agent - Auto-Startup Registry Registration Script
param (
    [switch]$Uninstall,
    [string]$ExePath = "$PSScriptRoot\..\dist\TrayApp\Agent.TrayApp.exe"
)

$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$ValueName = "ComputerDoctorAgent"

if ($Uninstall) {
    Write-Host "Removing ComputerDoctor Agent auto-startup key..." -ForegroundColor Yellow
    Remove-ItemProperty -Path $RegistryPath -Name $ValueName -ErrorAction SilentlyContinue
    Write-Host "Auto-startup registration removed." -ForegroundColor Green
} else {
    $ResolvedPath = System.IO.Path::GetFullPath($ExePath)
    Write-Host "Registering ComputerDoctor Agent auto-startup: $ResolvedPath" -ForegroundColor Cyan
    Set-ItemProperty -Path $RegistryPath -Name $ValueName -Value "`"$ResolvedPath`""
    Write-Host "Auto-startup registration complete! Value set under HKCU\Software\Microsoft\Windows\CurrentVersion\Run" -ForegroundColor Green
}
