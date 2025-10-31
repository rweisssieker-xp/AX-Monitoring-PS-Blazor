# Test script to debug module loading
Write-Host "Testing module imports..."

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import required modules
$ModulePath = Join-Path $PSScriptRoot 'Modules'
Write-Host "Module path: $ModulePath"

# Test each module import individually
try {
    Write-Host "Importing AXMonitor.Config..."
    $configModule = Import-Module (Join-Path $ModulePath 'AXMonitor.Config') -Force -PassThru
    Write-Host "Config module imported successfully. Available functions: $($configModule.ExportedFunctions.Keys -join ', ')"
}
catch {
    Write-Host "Error importing config module: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

try {
    Write-Host "Testing Initialize-AXMonitorConfig function..."
    $testConfig = Initialize-AXMonitorConfig -Environment 'DEV'
    Write-Host "Initialize-AXMonitorConfig works!" -ForegroundColor Green
}
catch {
    Write-Host "Error calling Initialize-AXMonitorConfig: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "All tests passed!" -ForegroundColor Green