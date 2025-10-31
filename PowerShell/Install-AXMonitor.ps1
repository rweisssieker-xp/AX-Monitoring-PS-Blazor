<#
.SYNOPSIS
    Installation script for AX Monitor PowerShell Edition
.DESCRIPTION
    Installs required modules, initializes database, and configures the monitoring system
.PARAMETER Environment
    Target environment (DEV, TST, PRD)
.PARAMETER SkipDatabaseInit
    Skip database initialization
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('DEV', 'TST', 'PRD')]
    [string]$Environment = 'DEV',
    
    [Parameter()]
    [switch]$SkipDatabaseInit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host @"
╔═══════════════════════════════════════════════════════════╗
║   AX Monitor Installation - PowerShell Edition           ║
╚═══════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Check PowerShell version
Write-Host "`n[1/7] Checking PowerShell version..." -ForegroundColor Green
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -lt 7) {
    Write-Host "❌ PowerShell 7+ is required. Current version: $psVersion" -ForegroundColor Red
    Write-Host "   Download from: https://github.com/PowerShell/PowerShell/releases" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ PowerShell version: $psVersion" -ForegroundColor Green

# Check and install Pode module
Write-Host "`n[2/7] Checking Pode module..." -ForegroundColor Green
if (-not (Get-Module -ListAvailable -Name Pode)) {
    Write-Host "   Installing Pode module..." -ForegroundColor Yellow
    try {
        Install-Module -Name Pode -Scope CurrentUser -Force -AllowClobber
        Write-Host "✅ Pode module installed" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to install Pode: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "✅ Pode module already installed" -ForegroundColor Green
}

# Check SQL Server ODBC Driver
Write-Host "`n[3/7] Checking SQL Server ODBC Driver..." -ForegroundColor Green
$odbcDrivers = Get-OdbcDriver | Where-Object { $_.Name -like '*SQL Server*' }
if ($odbcDrivers) {
    Write-Host "✅ SQL Server ODBC Driver found: $($odbcDrivers[0].Name)" -ForegroundColor Green
}
else {
    Write-Host "⚠️  No SQL Server ODBC Driver found" -ForegroundColor Yellow
    Write-Host "   Download from: https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server" -ForegroundColor Yellow
}

# Create directory structure
Write-Host "`n[4/7] Creating directory structure..." -ForegroundColor Green
$directories = @(
    'Logs',
    'Config',
    'Public/css',
    'Public/js',
    'Public/images',
    'Views'
)

foreach ($dir in $directories) {
    $path = Join-Path $PSScriptRoot $dir
    if (-not (Test-Path $path)) {
        New-Item -Path $path -ItemType Directory -Force | Out-Null
        Write-Host "   Created: $dir" -ForegroundColor Gray
    }
}
Write-Host "✅ Directory structure created" -ForegroundColor Green

# Check configuration file
Write-Host "`n[5/7] Checking configuration..." -ForegroundColor Green
$configFile = Join-Path $PSScriptRoot "Config\.env.$Environment"
if (-not (Test-Path $configFile)) {
    Write-Host "⚠️  Configuration file not found: .env.$Environment" -ForegroundColor Yellow
    Write-Host "   Creating from template..." -ForegroundColor Yellow
    
    $exampleFile = Join-Path $PSScriptRoot "Config\env.example"
    if (Test-Path $exampleFile) {
        Copy-Item $exampleFile $configFile
        Write-Host "✅ Configuration file created: $configFile" -ForegroundColor Green
        Write-Host "   ⚠️  IMPORTANT: Edit this file with your database credentials!" -ForegroundColor Yellow
    }
    else {
        Write-Host "❌ Example configuration file not found" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "✅ Configuration file exists: .env.$Environment" -ForegroundColor Green
}

# Initialize database
if (-not $SkipDatabaseInit) {
    Write-Host "`n[6/7] Initializing staging database..." -ForegroundColor Green
    try {
        # Import modules
        $ModulePath = Join-Path $PSScriptRoot 'Modules'
        Import-Module (Join-Path $ModulePath 'AXMonitor.Config') -Force
        Import-Module (Join-Path $ModulePath 'AXMonitor.Database') -Force
        
        # Load configuration
        $Config = Initialize-AXMonitorConfig -Environment $Environment
        
        # Test connection
        Write-Host "   Testing database connection..." -ForegroundColor Gray
        $dbTest = Test-AXDatabaseConnection -Config $Config
        if (-not $dbTest.Success) {
            Write-Host "❌ Database connection failed: $($dbTest.Error)" -ForegroundColor Red
            Write-Host "   Please check your configuration in: $configFile" -ForegroundColor Yellow
            exit 1
        }
        
        # Initialize schema
        Write-Host "   Creating database schema..." -ForegroundColor Gray
        Initialize-StagingDatabase -Config $Config
        
        Write-Host "✅ Database initialized successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Database initialization failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   You can skip this step with -SkipDatabaseInit and run it manually later" -ForegroundColor Yellow
        exit 1
    }
}
else {
    Write-Host "`n[6/7] Skipping database initialization" -ForegroundColor Yellow
}

# Create Windows Service script (optional)
Write-Host "`n[7/7] Creating service installation script..." -ForegroundColor Green
$serviceScript = @"
# Install AX Monitor as Windows Service using NSSM
# Download NSSM from: https://nssm.cc/download

`$serviceName = 'AXMonitor'
`$scriptPath = '$PSScriptRoot\Start-AXMonitor.ps1'
`$pwshPath = (Get-Command pwsh).Source

# Install service
nssm install `$serviceName `$pwshPath
nssm set `$serviceName AppParameters "-File `"`$scriptPath`" -Environment $Environment"
nssm set `$serviceName AppDirectory '$PSScriptRoot'
nssm set `$serviceName DisplayName 'AX Monitor - Performance Monitoring'
nssm set `$serviceName Description 'Real-time monitoring for Microsoft Dynamics AX 2012 R3'
nssm set `$serviceName Start SERVICE_AUTO_START

Write-Host "Service installed. Start with: nssm start `$serviceName"
"@

$serviceScriptPath = Join-Path $PSScriptRoot 'Install-Service.ps1'
$serviceScript | Out-File -FilePath $serviceScriptPath -Encoding UTF8
Write-Host "✅ Service script created: Install-Service.ps1" -ForegroundColor Green

# Installation complete
Write-Host @"

╔═══════════════════════════════════════════════════════════╗
║   Installation Complete!                                 ║
╚═══════════════════════════════════════════════════════════╝

Next Steps:
1. Edit configuration file: $configFile
2. Configure database credentials
3. (Optional) Configure OpenAI API key for AI features
4. Start the server:
   
   .\Start-AXMonitor.ps1 -Environment $Environment

5. Open browser: http://localhost:8080

For Windows Service installation:
   .\Install-Service.ps1

Documentation: See README.md for more information

"@ -ForegroundColor Cyan

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
