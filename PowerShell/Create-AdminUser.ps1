<#
.SYNOPSIS
    Create initial admin user for AX Monitor
.DESCRIPTION
    Creates an initial admin user for the AX Monitor system with authentication enabled
.NOTES
    Author: AX Monitoring Team
    Version: 1.0.0
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('DEV', 'TST', 'PRD')]
    [string]$Environment = 'DEV'
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import required modules
$ModulePath = Join-Path $PSScriptRoot 'Modules'
Import-Module (Join-Path $ModulePath 'AXMonitor.Config') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Database') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Auth') -Force

# Load configuration
Write-Host "🔧 Loading configuration for environment: $Environment" -ForegroundColor Green
$Config = Initialize-AXMonitorConfig -Environment $Environment

# Initialize authentication database
Write-Host "🔐 Initializing authentication database..." -ForegroundColor Green
Initialize-AXAuthDatabase -Config $Config

# Prompt for admin user details
Write-Host "`n📝 Creating initial admin user" -ForegroundColor Cyan
$Username = Read-Host "Enter admin username (default: admin)"
if ([string]::IsNullOrWhiteSpace($Username)) {
    $Username = "admin"
}

$Password = Read-Host "Enter admin password" -AsSecureString
$PasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))

$Email = Read-Host "Enter admin email (optional)"
if ([string]::IsNullOrWhiteSpace($Email)) {
    $Email = "$Username@axmonitor.local"
}

$FullName = Read-Host "Enter admin full name (optional)"
if ([string]::IsNullOrWhiteSpace($FullName)) {
    $FullName = "System Administrator"
}

try {
    # Create admin user
    Write-Host "`nCreating admin user..." -ForegroundColor Yellow
    $result = New-AXUser -Config $Config -Username $Username -Password $PasswordPlain -Role 'Admin' -Email $Email -FullName $FullName
    
    if ($result) {
        Write-Host "✅ Admin user '$Username' created successfully!" -ForegroundColor Green
        Write-Host "🔐 You can now log in with this account" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Failed to create admin user" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error creating admin user: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}