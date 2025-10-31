# Install AX Monitor as Windows Service using NSSM
# Download NSSM from: https://nssm.cc/download

$serviceName = 'AXMonitor'
$scriptPath = 'D:\temp\AXMonitoringBU_Powershell\PowerShell\Start-AXMonitor.ps1'
$pwshPath = (Get-Command pwsh).Source

# Install service
nssm install $serviceName $pwshPath
nssm set $serviceName AppParameters "-File "$scriptPath" -Environment DEV"
nssm set $serviceName AppDirectory 'D:\temp\AXMonitoringBU_Powershell\PowerShell'
nssm set $serviceName DisplayName 'AX Monitor - Performance Monitoring'
nssm set $serviceName Description 'Real-time monitoring for Microsoft Dynamics AX 2012 R3'
nssm set $serviceName Start SERVICE_AUTO_START

Write-Host "Service installed. Start with: nssm start $serviceName"
