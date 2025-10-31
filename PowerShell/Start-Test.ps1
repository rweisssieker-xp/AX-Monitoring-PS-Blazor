# Minimal Pode Test Server

param([int]$Port = 8080)

# Install Pode if needed
if (-not (Get-Module -ListAvailable -Name Pode)) {
    Write-Host "Installing Pode..." -ForegroundColor Yellow
    Install-Module -Name Pode -Scope CurrentUser -Force
}

Import-Module Pode

Write-Host "`nStarting test server on port $Port..." -ForegroundColor Green
Write-Host "Open: http://localhost:$Port/api/health`n" -ForegroundColor Cyan

Start-PodeServer -Threads 2 {
    Add-PodeEndpoint -Address localhost -Port 8080 -Protocol Http
    
    Add-PodeRoute -Method Get -Path '/api/health' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            status = 'OK'
            message = 'AX Monitor is running!'
            timestamp = (Get-Date).ToString('o')
            powershell = $PSVersionTable.PSVersion.ToString()
        }
    }
    
    Add-PodeRoute -Method Get -Path '/' -ScriptBlock {
        Write-PodeTextResponse -Value 'AX Monitor Test Server - Running! Try /api/health for JSON response'
    }
    
    Add-PodeRoute -Method Get -Path '/test' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            message = 'Test endpoint working'
            server = 'Pode'
            time = Get-Date -Format 'HH:mm:ss'
        }
    }
}
