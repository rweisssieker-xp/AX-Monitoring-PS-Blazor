<#
.SYNOPSIS
    AX 2012 R3 Performance Monitor - Simplified Starter
.DESCRIPTION
    Simplified version to test the Pode server
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('DEV', 'TST', 'PRD')]
    [string]$Environment = 'DEV',
    
    [Parameter()]
    [int]$Port = 8080
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host @"
╔═══════════════════════════════════════════════════════════╗
║   AX 2012 R3 Performance Monitor - PowerShell Edition    ║
╚═══════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Check if Pode is installed
if (-not (Get-Module -ListAvailable -Name Pode)) {
    Write-Host "❌ Pode module not found. Installing..." -ForegroundColor Yellow
    Install-Module -Name Pode -Scope CurrentUser -Force
}

Import-Module Pode -Force

Write-Host "`n✅ Starting AX Monitor Server..." -ForegroundColor Green
Write-Host "🌐 Dashboard: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "🔧 Environment: $Environment" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop...`n" -ForegroundColor Yellow

# Start Pode server
Start-PodeServer {
    
    # Add endpoint
    Add-PodeEndpoint -Address localhost -Port $using:Port -Protocol Http
    
    # Enable logging
    New-PodeLoggingMethod -Terminal | Enable-PodeErrorLogging
    
    # Home page
    Add-PodeRoute -Method Get -Path '/' -ScriptBlock {
        Write-PodeHtmlResponse -Value @"
<!DOCTYPE html>
<html>
<head>
    <title>AX Monitor</title>
    <style>
        body { font-family: Arial; margin: 40px; background: #f5f7fa; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; }
        .status { color: #28a745; font-size: 24px; }
        .info { background: #e3f2fd; padding: 20px; border-radius: 8px; margin: 20px 0; }
        a { color: #1f77b4; text-decoration: none; }
        a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class="container">
        <h1>📊 AX Monitor - PowerShell Edition</h1>
        <p class="status">✅ Server is running!</p>
        
        <div class="info">
            <h3>🚀 Quick Links</h3>
            <ul>
                <li><a href="/api/health">Health Check</a></li>
                <li><a href="/api/test">Test API</a></li>
            </ul>
        </div>
        
        <div class="info">
            <h3>📝 Next Steps</h3>
            <ol>
                <li>Configure database connection in Config\.env.DEV</li>
                <li>Import monitoring modules</li>
                <li>Access full dashboard</li>
            </ol>
        </div>
        
        <p><strong>Environment:</strong> $($using:Environment)</p>
        <p><strong>Port:</strong> $($using:Port)</p>
        <p><strong>Framework:</strong> Pode $(Get-Module Pode | Select-Object -ExpandProperty Version)</p>
    </div>
</body>
</html>
"@
    }
    
    # Health check API
    Add-PodeRoute -Method Get -Path '/api/health' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            status = 'healthy'
            timestamp = Get-Date -Format 'o'
            version = '2.0.0'
            environment = $using:Environment
        }
    }
    
    # Test API
    Add-PodeRoute -Method Get -Path '/api/test' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            message = 'AX Monitor API is working!'
            server = 'Pode'
            powershell = $PSVersionTable.PSVersion.ToString()
        }
    }
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ✅ Server started successfully!" -ForegroundColor Green
}
