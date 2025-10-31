# Test if modules can be loaded

Write-Host "`nTesting AX Monitor Modules..." -ForegroundColor Cyan
Write-Host "=" * 50

$modules = @(
    'AXMonitor.Config',
    'AXMonitor.Database',
    'AXMonitor.Monitoring',
    'AXMonitor.Alerts',
    'AXMonitor.AI'
)

foreach ($moduleName in $modules) {
    $modulePath = Join-Path $PSScriptRoot "Modules\$moduleName\$moduleName.psm1"
    
    Write-Host "`nTesting: $moduleName" -ForegroundColor Yellow
    Write-Host "Path: $modulePath"
    
    if (Test-Path $modulePath) {
        try {
            Import-Module $modulePath -Force -ErrorAction Stop
            Write-Host "  [OK] Module loaded successfully" -ForegroundColor Green
            
            # List exported functions
            $functions = Get-Command -Module $moduleName -ErrorAction SilentlyContinue
            if ($functions) {
                Write-Host "  Functions: $($functions.Count)" -ForegroundColor Cyan
            }
        }
        catch {
            Write-Host "  [ERROR] $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "  [NOT FOUND] File does not exist" -ForegroundColor Red
    }
}

Write-Host "`n" + ("=" * 50)
Write-Host "Module test complete!`n" -ForegroundColor Cyan
