# Start AX Monitor with PowerShell 7

$pwsh7Path = "C:\Program Files\PowerShell\7\pwsh.exe"

if (Test-Path $pwsh7Path) {
    Write-Host "Starting with PowerShell 7..." -ForegroundColor Green
    & $pwsh7Path -File "$PSScriptRoot\Start-AXMonitor-Working.ps1" -Environment DEV -Port 9090
}
else {
    Write-Host "PowerShell 7 not found at: $pwsh7Path" -ForegroundColor Red
    Write-Host "Please install PowerShell 7 first: winget install Microsoft.PowerShell" -ForegroundColor Yellow
}
