<#
.SYNOPSIS
    AX 2012 R3 Performance Monitor - Working Version
.DESCRIPTION
    Starts Pode web server with REST API endpoints
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('DEV', 'TST', 'PRD')]
    [string]$Environment = 'DEV',
    
    [Parameter()]
    [int]$Port = 8080,
    
    [Parameter()]
    [switch]$EnableOpenAI
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AX Monitor - PowerShell Edition" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Install Pode if needed
if (-not (Get-Module -ListAvailable -Name Pode)) {
    Write-Host "Installing Pode..." -ForegroundColor Yellow
    Install-Module -Name Pode -Scope CurrentUser -Force
}

Import-Module Pode -Force

# Try to load AX Monitor modules
$ModulePath = Join-Path $PSScriptRoot 'Modules'
$loadedModules = @()

foreach ($moduleName in @('AXMonitor.Config', 'AXMonitor.Database', 'AXMonitor.Monitoring', 'AXMonitor.Alerts', 'AXMonitor.AI')) {
    $moduleFile = Join-Path $ModulePath "$moduleName\$moduleName.psm1"
    if (Test-Path $moduleFile) {
        try {
            Import-Module $moduleFile -Force -ErrorAction Stop
            $loadedModules += $moduleName
            Write-Host "  [OK] $moduleName" -ForegroundColor Green
        }
        catch {
            Write-Host "  [SKIP] $moduleName - $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# Try to load configuration
$Config = $null
$configLoaded = $false

if ('AXMonitor.Config' -in $loadedModules) {
    try {
        $Config = Initialize-AXMonitorConfig -Environment $Environment
        $configLoaded = $true
        Write-Host "  [OK] Configuration loaded" -ForegroundColor Green
        
        # Test database
        if ('AXMonitor.Database' -in $loadedModules) {
            $dbTest = Test-AXDatabaseConnection -Config $Config
            if ($dbTest.Success) {
                Write-Host "  [OK] Database connection" -ForegroundColor Green
            }
            else {
                Write-Host "  [WARN] Database: $($dbTest.Error)" -ForegroundColor Yellow
            }
        }
    }
    catch {
        Write-Host "  [WARN] Config failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`nStarting server on port $Port..." -ForegroundColor Green
Write-Host "Dashboard: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "API: http://localhost:$Port/api/health`n" -ForegroundColor Cyan

# Start Pode server
Start-PodeServer -Threads 2 -ScriptBlock {
    
    Add-PodeEndpoint -Address localhost -Port $Port -Protocol Http
    
    New-PodeLoggingMethod -Terminal | Enable-PodeErrorLogging
    
    # Store state - variables from parent scope are accessible here
    Set-PodeState -Name 'Config' -Value $Config
    Set-PodeState -Name 'Environment' -Value $Environment
    Set-PodeState -Name 'ConfigLoaded' -Value $configLoaded
    Set-PodeState -Name 'LoadedModules' -Value $loadedModules
    
    # Home page - HTML Dashboard
    Add-PodeRoute -Method Get -Path '/' -ScriptBlock {
        $env = Get-PodeState -Name 'Environment'
        $cfgLoaded = Get-PodeState -Name 'ConfigLoaded'
        $modules = Get-PodeState -Name 'LoadedModules'
        
        $html = @"
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AX Monitor Dashboard</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            color: #333;
        }
        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
        }
        header {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 25px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        h1 {
            color: #667eea;
            font-size: 2.2em;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 15px;
            font-weight: 700;
        }
        .status-badge {
            display: inline-block;
            padding: 8px 16px;
            background: #10b981;
            color: white;
            border-radius: 20px;
            font-size: 0.4em;
            font-weight: 600;
            animation: pulse 2s infinite;
        }
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.7; }
        }
        nav {
            display: flex;
            gap: 10px;
            margin-top: 20px;
            flex-wrap: wrap;
        }
        .nav-btn {
            padding: 12px 24px;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            font-size: 14px;
            font-weight: 600;
            transition: all 0.3s;
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
        }
        .nav-btn:hover {
            background: #5568d3;
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
        }
        .nav-btn.active {
            background: #764ba2;
        }
        .dashboard-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
            margin-bottom: 25px;
        }
        .card {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s;
        }
        .card:hover {
            transform: translateY(-5px);
        }
        .card h2 {
            color: #667eea;
            margin-bottom: 15px;
            font-size: 1.1em;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .metric {
            font-size: 2.8em;
            font-weight: 700;
            color: #764ba2;
            margin: 15px 0;
        }
        .metric-label {
            color: #666;
            font-size: 1em;
            text-transform: uppercase;
            letter-spacing: 1px;
            font-weight: 500;
        }
        .content-section {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            display: none;
        }
        .content-section.active {
            display: block;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
        }
        th, td {
            padding: 14px;
            text-align: left;
            border-bottom: 1px solid #e5e7eb;
            font-size: 1em;
        }
        th {
            background: #f9fafb;
            color: #667eea;
            font-weight: 700;
            font-size: 1.05em;
        }
        tr:hover {
            background: #f9fafb;
        }
        .loading {
            text-align: center;
            padding: 40px;
            color: #666;
        }
        .spinner {
            border: 3px solid #f3f3f3;
            border-top: 3px solid #667eea;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }
        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        .alert {
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0;
        }
        .alert-warning {
            background: #fef3c7;
            border-left: 4px solid #f59e0b;
            color: #92400e;
        }
        .alert-error {
            background: #fee2e2;
            border-left: 4px solid #ef4444;
            color: #991b1b;
        }
        .alert-success {
            background: #d1fae5;
            border-left: 4px solid #10b981;
            color: #065f46;
        }
        footer {
            text-align: center;
            color: rgba(255, 255, 255, 0.8);
            margin-top: 30px;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>
                AX Monitor Dashboard
                <span class="status-badge">LIVE</span>
            </h1>
            <p style="color: #666; margin-top: 10px; font-size: 1.1em;">
                Environment: <strong>$env</strong> | 
                Config: <strong>$(if($cfgLoaded){'Loaded'}else{'Demo Mode'})</strong> | 
                Modules: <strong>$($modules.Count)</strong>
            </p>
            <nav>
                <button class="nav-btn active" onclick="showSection('overview')">Overview</button>
                <button class="nav-btn" onclick="showSection('kpi')">KPIs</button>
                <button class="nav-btn" onclick="showSection('batch')">Batch Jobs</button>
                <button class="nav-btn" onclick="showSection('sessions')">Sessions</button>
                <button class="nav-btn" onclick="showSection('alerts')">Alerts</button>
                <button class="nav-btn" onclick="showSection('ai')">AI Analysis</button>
            </nav>
        </header>

        <div id="overview" class="content-section active">
            <div class="dashboard-grid" id="kpi-cards">
                <div class="card">
                    <div class="loading">
                        <div class="spinner"></div>
                        <p>Loading KPI data...</p>
                    </div>
                </div>
            </div>
        </div>

        <div id="kpi" class="content-section">
            <div class="card">
                <h2>Key Performance Indicators</h2>
                <div id="kpi-content" class="loading">
                    <div class="spinner"></div>
                    <p>Loading...</p>
                </div>
            </div>
        </div>

        <div id="batch" class="content-section">
            <div class="card">
                <h2>Batch Jobs</h2>
                <div id="batch-content" class="loading">
                    <div class="spinner"></div>
                    <p>Loading...</p>
                </div>
            </div>
        </div>

        <div id="sessions" class="content-section">
            <div class="card">
                <h2>Active Sessions</h2>
                <div id="sessions-content" class="loading">
                    <div class="spinner"></div>
                    <p>Loading...</p>
                </div>
            </div>
        </div>

        <div id="alerts" class="content-section">
            <div class="card">
                <h2>System Alerts</h2>
                <div id="alerts-content" class="loading">
                    <div class="spinner"></div>
                    <p>Loading...</p>
                </div>
            </div>
        </div>

        <div id="ai" class="content-section">
            <div class="card">
                <h2>AI-Powered Analysis</h2>
                <div id="ai-content" class="loading">
                    <div class="spinner"></div>
                    <p>Loading AI analysis...</p>
                </div>
            </div>
        </div>

        <footer>
            <p>AX Monitor v2.0.0 | Powered by Pode & PowerShell | Last updated: <span id="last-update"></span></p>
        </footer>
    </div>

    <script>
        let currentSection = 'overview';
        let autoRefresh = true;

        function showSection(section) {
            document.querySelectorAll('.content-section').forEach(s => s.classList.remove('active'));
            document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
            
            document.getElementById(section).classList.add('active');
            event.target.classList.add('active');
            
            currentSection = section;
            loadSectionData(section);
        }

        async function loadSectionData(section) {
            const timestamp = new Date().toLocaleTimeString('de-DE');
            document.getElementById('last-update').textContent = timestamp;

            try {
                switch(section) {
                    case 'overview':
                        await loadKPICards();
                        break;
                    case 'kpi':
                        await loadKPIDetails();
                        break;
                    case 'batch':
                        await loadBatchJobs();
                        break;
                    case 'sessions':
                        await loadSessions();
                        break;
                    case 'alerts':
                        await loadAlerts();
                        break;
                    case 'ai':
                        await loadAIAnalysis();
                        break;
                }
            } catch (error) {
                console.error('Error loading data:', error);
            }
        }

        async function loadKPICards() {
            const response = await fetch('/api/kpi');
            const data = await response.json();
            
            const html = \`
                <div class="card">
                    <h2>Batch Backlog</h2>
                    <div class="metric">\${data.BatchBacklog || 0}</div>
                    <div class="metric-label">Pending Jobs</div>
                </div>
                <div class="card">
                    <h2>Error Rate</h2>
                    <div class="metric">\${(data.ErrorRate || 0).toFixed(1)}%</div>
                    <div class="metric-label">Last 24h</div>
                </div>
                <div class="card">
                    <h2>Active Sessions</h2>
                    <div class="metric">\${data.ActiveSessions || 0}</div>
                    <div class="metric-label">Current Users</div>
                </div>
                <div class="card">
                    <h2>Blocking Chains</h2>
                    <div class="metric">\${data.BlockingChains || 0}</div>
                    <div class="metric-label">Database Locks</div>
                </div>
                <div class="card">
                    <h2>CPU Usage</h2>
                    <div class="metric">\${(data.CPUUsage || 0).toFixed(1)}%</div>
                    <div class="metric-label">Server Load</div>
                </div>
                <div class="card">
                    <h2>Memory Usage</h2>
                    <div class="metric">\${(data.MemoryUsage || 0).toFixed(1)}%</div>
                    <div class="metric-label">RAM Utilization</div>
                </div>
            \`;
            
            document.getElementById('kpi-cards').innerHTML = html;
        }

        async function loadKPIDetails() {
            const response = await fetch('/api/kpi');
            const data = await response.json();
            
            const html = \`
                <table>
                    <tr><th>Metric</th><th>Value</th><th>Status</th></tr>
                    <tr><td>Batch Backlog</td><td>\${data.BatchBacklog || 0}</td><td>\${data.BatchBacklog > 10 ? 'WARNING' : 'OK'}</td></tr>
                    <tr><td>Error Rate</td><td>\${(data.ErrorRate || 0).toFixed(2)}%</td><td>\${data.ErrorRate > 5 ? 'WARNING' : 'OK'}</td></tr>
                    <tr><td>Active Sessions</td><td>\${data.ActiveSessions || 0}</td><td>OK</td></tr>
                    <tr><td>Blocking Chains</td><td>\${data.BlockingChains || 0}</td><td>\${data.BlockingChains > 0 ? 'ERROR' : 'OK'}</td></tr>
                    <tr><td>CPU Usage</td><td>\${(data.CPUUsage || 0).toFixed(1)}%</td><td>\${data.CPUUsage > 80 ? 'WARNING' : 'OK'}</td></tr>
                    <tr><td>Memory Usage</td><td>\${(data.MemoryUsage || 0).toFixed(1)}%</td><td>\${data.MemoryUsage > 85 ? 'WARNING' : 'OK'}</td></tr>
                    <tr><td>Active Connections</td><td>\${data.ActiveConnections || 0}</td><td>OK</td></tr>
                </table>
                $([char]36){data.Mode ? \`<div class="alert alert-warning" style="margin-top: 20px;">INFO: Running in <strong>$([char]36){data.Mode}</strong> mode</div>\` : ''}
            \`;
            
            document.getElementById('kpi-content').innerHTML = html;
        }

        async function loadBatchJobs() {
            const response = await fetch('/api/batch');
            const data = await response.json();
            
            let html = '';
            if (data.count > 0) {
                html = '<table><tr><th>Job Name</th><th>Status</th><th>Duration</th><th>Last Run</th></tr>';
                data.data.forEach(job => {
                    html += \`<tr>
                        <td>\${job.JobName || 'N/A'}</td>
                        <td>\${job.Status || 'N/A'}</td>
                        <td>\${job.Duration || 'N/A'}</td>
                        <td>\${job.LastRun || 'N/A'}</td>
                    </tr>\`;
                });
                html += '</table>';
            } else {
                html = \`<div class="alert alert-warning">INFO: \${data.mode || 'No batch jobs found'}</div>\`;
            }
            
            document.getElementById('batch-content').innerHTML = html;
        }

        async function loadSessions() {
            const response = await fetch('/api/sessions');
            const data = await response.json();
            
            let html = '';
            if (data.count > 0) {
                html = '<table><tr><th>User</th><th>Session ID</th><th>Status</th><th>Last Activity</th></tr>';
                data.data.forEach(session => {
                    html += \`<tr>
                        <td>\${session.UserName || 'N/A'}</td>
                        <td>\${session.SessionId || 'N/A'}</td>
                        <td>\${session.Status || 'N/A'}</td>
                        <td>\${session.LastActivity || 'N/A'}</td>
                    </tr>\`;
                });
                html += '</table>';
            } else {
                html = \`<div class="alert alert-warning">INFO: \${data.mode || 'No active sessions'}</div>\`;
            }
            
            document.getElementById('sessions-content').innerHTML = html;
        }

        async function loadAlerts() {
            const response = await fetch('/api/alerts');
            const data = await response.json();
            
            let html = '';
            if (data.count > 0) {
                data.data.forEach(alert => {
                    const alertClass = alert.Severity === 'Critical' ? 'alert-error' : 
                                     alert.Severity === 'Warning' ? 'alert-warning' : 'alert-success';
                    html += \`<div class="alert \${alertClass}">
                        <strong>\${alert.Severity || 'Info'}:</strong> \${alert.Message || 'No message'}
                        <br><small>\${alert.Timestamp || ''}</small>
                    </div>\`;
                });
            } else {
                html = '<div class="alert alert-success">OK: No active alerts - All systems operational</div>';
            }
            
            document.getElementById('alerts-content').innerHTML = html;
        }

        async function loadAIAnalysis() {
            const response = await fetch('/api/ai-analysis');
            const data = await response.json();
            
            let html = '';
            if (data.status === 'Error') {
                html = \`<div class="alert alert-warning">WARNING: \${data.message}</div>\`;
            } else {
                html = \`
                    <div class="alert alert-success">
                        <strong>AI Analysis Results</strong>
                        <p>Analysis completed successfully. Check the data below for insights.</p>
                    </div>
                    <pre style="background: #f9fafb; padding: 15px; border-radius: 8px; overflow-x: auto;">\${JSON.stringify(data, null, 2)}</pre>
                \`;
            }
            
            document.getElementById('ai-content').innerHTML = html;
        }

        // Auto-refresh every 30 seconds
        setInterval(() => {
            if (autoRefresh) {
                loadSectionData(currentSection);
            }
        }, 30000);

        // Initial load
        loadSectionData('overview');
    </script>
</body>
</html>
"@
        Write-PodeHtmlResponse -Value $html
    }
    
    # Health check
    Add-PodeRoute -Method Get -Path '/api/health' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            status = 'healthy'
            timestamp = (Get-Date).ToString('o')
            version = '2.0.0'
            environment = Get-PodeState -Name 'Environment'
            configLoaded = Get-PodeState -Name 'ConfigLoaded'
            loadedModules = Get-PodeState -Name 'LoadedModules'
            powershell = $PSVersionTable.PSVersion.ToString()
            pode = (Get-Module Pode).Version.ToString()
        }
    }
    
    # KPI endpoint
    Add-PodeRoute -Method Get -Path '/api/kpi' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Get-AXKPIData -ErrorAction SilentlyContinue)) {
            try {
                $kpi = Get-AXKPIData -Config $cfg
                Write-PodeJsonResponse -Value $kpi
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            # Demo data
            Write-PodeJsonResponse -Value @{
                BatchBacklog = 5
                ErrorRate = 2.5
                ActiveSessions = 42
                BlockingChains = 0
                CPUUsage = 45.2
                MemoryUsage = 62.8
                ActiveConnections = 128
                Timestamp = (Get-Date).ToString('o')
                Mode = 'Demo'
            }
        }
    }
    
    # Batch jobs endpoint
    Add-PodeRoute -Method Get -Path '/api/batch' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Get-AXBatchJobs -ErrorAction SilentlyContinue)) {
            try {
                $jobs = Get-AXBatchJobs -Config $cfg
                Write-PodeJsonResponse -Value @{ 
                    data = $jobs
                    count = $jobs.Count 
                }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            Write-PodeJsonResponse -Value @{ 
                data = @()
                count = 0
                mode = 'Demo - Configure database'
            }
        }
    }
    
    # Sessions endpoint
    Add-PodeRoute -Method Get -Path '/api/sessions' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Get-AXSessions -ErrorAction SilentlyContinue)) {
            try {
                $sessions = Get-AXSessions -Config $cfg
                Write-PodeJsonResponse -Value @{ 
                    data = $sessions
                    count = $sessions.Count 
                }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            Write-PodeJsonResponse -Value @{ 
                data = @()
                count = 0
                mode = 'Demo'
            }
        }
    }
    
    # Alerts endpoint
    Add-PodeRoute -Method Get -Path '/api/alerts' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Get-AXAlerts -ErrorAction SilentlyContinue)) {
            try {
                $alerts = Get-AXAlerts -Config $cfg
                Write-PodeJsonResponse -Value @{ 
                    data = $alerts
                    count = $alerts.Count 
                }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            Write-PodeJsonResponse -Value @{ 
                data = @()
                count = 0
                mode = 'Demo'
            }
        }
    }
    
    # AI Analysis endpoint
    Add-PodeRoute -Method Get -Path '/api/ai-analysis' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Get-AIAnalysis -ErrorAction SilentlyContinue)) {
            try {
                # Get metrics from monitoring module
                $metrics = @{
                    batch = @()
                    sessions = @()
                    blocking = @()
                    sqlHealth = @()
                }
                
                # Populate metrics if available
                if (Get-Command Get-AXBatchJobs -ErrorAction SilentlyContinue) {
                    $batchJobs = Get-AXBatchJobs -Config $cfg
                    $metrics.batch = $batchJobs | ForEach-Object { 
                        @{ 
                            timestamp = $_.ExecutedTime 
                            value = $_.DurationSeconds 
                        } 
                    }
                }
                
                if (Get-Command Get-AXSessions -ErrorAction SilentlyContinue) {
                    $sessions = Get-AXSessions -Config $cfg
                    $metrics.sessions = $sessions | ForEach-Object { 
                        @{ 
                            timestamp = $_.LastActivityTime 
                            value = $_.SessionCount 
                        } 
                    }
                }
                
                # Perform AI analysis
                $analysis = Get-AIAnalysis -Metrics $metrics -TimeWindow "24h"
                
                Write-PodeJsonResponse -Value $analysis
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            Write-PodeJsonResponse -Value @{ 
                status = "Error"
                message = "AI analysis not available - check module loading"
                data = @{}
            }
        }
    }
    
    # Anomaly detection endpoint
    Add-PodeRoute -Method Get -Path '/api/anomalies' -ScriptBlock {
        $cfg = Get-PodeState -Name 'Config'
        
        if ($cfg -and (Get-Command Detect-Anomalies -ErrorAction SilentlyContinue)) {
            try {
                # Get metrics from monitoring module
                $metrics = @{
                    batch = @()
                    sessions = @()
                    blocking = @()
                    sqlHealth = @()
                }
                
                # Populate metrics if available
                if (Get-Command Get-AXBatchJobs -ErrorAction SilentlyContinue) {
                    $batchJobs = Get-AXBatchJobs -Config $cfg
                    $metrics.batch = $batchJobs | ForEach-Object { 
                        @{ 
                            timestamp = $_.ExecutedTime 
                            value = $_.DurationSeconds 
                        } 
                    }
                }
                
                if (Get-Command Get-AXSessions -ErrorAction SilentlyContinue) {
                    $sessions = Get-AXSessions -Config $cfg
                    $metrics.sessions = $sessions | ForEach-Object { 
                        @{ 
                            timestamp = $_.LastActivityTime 
                            value = $_.SessionCount 
                        } 
                    }
                }
                
                # Detect anomalies
                $anomalies = Detect-Anomalies -Metrics ($metrics.batch + $metrics.sessions) -Method "ZScore" -Threshold 95
                
                Write-PodeJsonResponse -Value $anomalies
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        else {
            Write-PodeJsonResponse -Value @{ 
                status = "Error"
                message = "Anomaly detection not available - check module loading"
                data = @{}
            }
        }
    }
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Server started!" -ForegroundColor Green
}
