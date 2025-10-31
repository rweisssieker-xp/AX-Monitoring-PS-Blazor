[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('DEV', 'TST', 'PRD')]
    [string]$Environment = 'DEV',
    
    [Parameter()]
    [int]$Port = 8080,
    
    [Parameter()]
    [switch]$EnableOpenAI,
    
    [Parameter()]
    [switch]$DebugMode
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import required modules
$ModulePath = Join-Path $PSScriptRoot 'Modules'
Import-Module (Join-Path $ModulePath 'AXMonitor.Config') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Database') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Monitoring') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Alerts') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.AI') -Force
Import-Module (Join-Path $ModulePath 'AXMonitor.Auth') -Force

# Check if Pode is installed
if (-not (Get-Module -ListAvailable -Name Pode)) {
    Write-Host "[ERROR] Pode module not found. Installing..." -ForegroundColor Yellow
    Install-Module -Name Pode -Scope CurrentUser -Force
}

Import-Module Pode -Force

Write-Host @"
===============================================
   AX 2012 R3 Performance Monitor - PowerShell Edition    
   Powered by Pode Web Framework                          
===============================================
"@ -ForegroundColor Cyan

# Load configuration
Write-Host "[CONFIG] Loading configuration for environment: $Environment" -ForegroundColor Green
$Config = Initialize-AXMonitorConfig -Environment $Environment

# Validate database connectivity
Write-Host "[DB] Testing database connections..." -ForegroundColor Green
$DbTest = Test-AXDatabaseConnection -Config $Config
if (-not $DbTest.Success) {
    Write-Host "[ERROR] Database connection failed: $($DbTest.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "[SUCCESS] Database connection successful" -ForegroundColor Green

# Display startup message
Write-Host "`n[STARTUP] Starting AX Monitor Server..." -ForegroundColor Green
Write-Host "[INFO] Dashboard will be available at: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "[INFO] Environment: $Environment" -ForegroundColor Cyan
if ($EnableOpenAI.IsPresent) {
    Write-Host "[INFO] AI Features: ENABLED" -ForegroundColor Magenta
}
Write-Host "`nPress Ctrl+C to stop the server...`n" -ForegroundColor Yellow

# Start Pode server
Start-PodeServer {
    
    # Add endpoints
    Add-PodeEndpoint -Address localhost -Port $Port -Protocol Http
    
    # Enable logging
    New-PodeLoggingMethod -Terminal | Enable-PodeErrorLogging
    New-PodeLoggingMethod -Terminal | Enable-PodeRequestLogging
    
    # Set view engine for HTML templates
    Set-PodeViewEngine -Type HTML -Extension HTML -ScriptBlock {
        param($path, $data)
        $content = Get-Content -Path $path -Raw
        foreach ($key in $data.Keys) {
            $content = $content -replace "{{$key}}", $data[$key]
        }
        return $content
    }
    
    # Enable sessions
    Enable-PodeSessionMiddleware -Duration 3600 -Extend
    
    # Add static content route
    Add-PodeStaticRoute -Path '/static' -Source (Join-Path $PSScriptRoot 'Public')
    
    # Store config in server state
    $PodeContext.Server.Data['Config'] = $Config
    $PodeContext.Server.Data['EnableOpenAI'] = $EnableOpenAI.IsPresent
    
    # Initialize authentication database if authentication is enabled
    if ($Config.Security.EnableAuthentication) {
        Write-Host "[AUTH] Initializing authentication database..." -ForegroundColor Green
        Initialize-AXAuthDatabase -Config $Config
    }
    
    # ============================================
    # Authentication Middleware
    # ============================================
    
    # Authentication middleware to check JWT tokens
    Add-PodeMiddleware -Name 'AuthMiddleware' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        
        # Skip authentication for login endpoint and static files
        if ($WebEvent.Route.Endpoint.Path -match '^/api/auth/login$' -or 
            $WebEvent.Route.Endpoint.Path -match '^/static/' -or
            $WebEvent.Route.Endpoint.Path -match '^/api/health$') {
            return $true
        }
        
        # Check if authentication is enabled
        if (-not $Config.Security.EnableAuthentication) {
            return $true
        }
        
        # Extract token from Authorization header
        $authHeader = $WebEvent.Request.Headers['Authorization']
        if ($authHeader -and $authHeader.StartsWith('Bearer ')) {
            $token = $authHeader.Substring(7)
        }
        else {
            # For web requests, also check for token in session
            $token = $WebEvent.Session.Data['AuthToken']
        }
        
        if (-not $token) {
            # Redirect to login for web requests
            if ($WebEvent.Request.Path -match '^(?!/api).*') {  # Not an API request
                Write-PodeRedirectResponse -Path '/login'
                return $false
            }
            
            # Return 401 for API requests
            Write-PodeJsonResponse -Value @{ error = 'Authentication required' } -StatusCode 401
            return $false
        }
        
        # Validate token
        $tokenData = Validate-AXJWTToken -Config $Config -Token $token
        if (-not $tokenData.IsValid) {
            # For web requests, redirect to login
            if ($WebEvent.Request.Path -match '^(?!/api).*') {  # Not an API request
                Write-PodeRedirectResponse -Path '/login'
                return $false
            }
            
            # Return 401 for API requests
            Write-PodeJsonResponse -Value @{ error = 'Invalid or expired token' } -StatusCode 401
            return $false
        }
        
        # Store user info in web event for use in routes
        $WebEvent.Data['User'] = $tokenData
        
        # Log API access
        if ($WebEvent.Request.Path -match '^/api/') {
            Log-APIAccess -Config $Config -UserId $tokenData.UserId -Endpoint $WebEvent.Request.Path -Method $WebEvent.Request.Method -IP $WebEvent.Request.RemoteEndPoint.Address -UserAgent $WebEvent.Request.Headers['User-Agent'] -ResponseCode 200
        }
        
        return $true
    }
    
    # ============================================
    # Web UI Routes
    # ============================================
    
    # Login page (no auth required)
    Add-PodeRoute -Method Get -Path '/login' -ScriptBlock {
        Write-PodeViewResponse -Path 'login.html'
    }
    
    # Home page
    Add-PodeRoute -Method Get -Path '/' -ScriptBlock {
        Write-PodeViewResponse -Path 'index.html' -Data @{
            Title = 'AX Monitor Dashboard'
            Environment = $PodeContext.Server.Data['Config'].Environment
            Version = '2.0.0'
        }
    }
    
    # Dashboard page - redirecting to home since index.html serves the dashboard
    Add-PodeRoute -Method Get -Path '/dashboard' -ScriptBlock {
        Write-PodeRedirectResponse -Path '/'
    }
    
    # Admin panel page (Admin only)
    Add-PodeRoute -Method Get -Path '/admin' -ScriptBlock {
        $user = $WebEvent.Data['User']
        
        # Admin role required
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Admin')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        Write-PodeViewResponse -Path 'admin.html' -Data @{
            Title = 'Admin Panel - AX Monitor'
            Environment = $PodeContext.Server.Data['Config'].Environment
            Version = '2.0.0'
        }
    }
    
    # Batch monitoring page
    Add-PodeRoute -Method Get -Path '/batch' -ScriptBlock {
        Write-PodeViewResponse -Path 'batch.html'
    }
    
    # Sessions page
    Add-PodeRoute -Method Get -Path '/sessions' -ScriptBlock {
        Write-PodeViewResponse -Path 'sessions.html'
    }
    
    # Blocking analysis page
    Add-PodeRoute -Method Get -Path '/blocking' -ScriptBlock {
        Write-PodeViewResponse -Path 'blocking.html'
    }
    
    # SQL Health page
    Add-PodeRoute -Method Get -Path '/sql-health' -ScriptBlock {
        Write-PodeViewResponse -Path 'sql-health.html'
    }
    
    # Alerts page
    Add-PodeRoute -Method Get -Path '/alerts' -ScriptBlock {
        Write-PodeViewResponse -Path 'alerts.html'
    }
    
    # AI Assistant page (if enabled)
    if ($PodeContext.Server.Data['EnableOpenAI']) {
        Add-PodeRoute -Method Get -Path '/ai-assistant' -ScriptBlock {
            Write-PodeViewResponse -Path 'ai-assistant.html'
        }
    }
    
    # ============================================
    # REST API Routes
    # ============================================
    
    # API: Get KPI summary
    Add-PodeRoute -Method Get -Path '/api/kpi' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $KpiData = Get-AXKPIData -Config $Config
            Write-PodeJsonResponse -Value $KpiData
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Get batch jobs
    Add-PodeRoute -Method Get -Path '/api/batch' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $BatchJobs = Get-AXBatchJobs -Config $Config
            Write-PodeJsonResponse -Value @{ data = $BatchJobs }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Get sessions
    Add-PodeRoute -Method Get -Path '/api/sessions' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $Sessions = Get-AXSessions -Config $Config
            Write-PodeJsonResponse -Value @{ data = $Sessions }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Get blocking chains
    Add-PodeRoute -Method Get -Path '/api/blocking' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $Blocking = Get-AXBlockingChains -Config $Config
            Write-PodeJsonResponse -Value @{ data = $Blocking }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Get SQL health metrics
    Add-PodeRoute -Method Get -Path '/api/sql-health' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $Health = Get-SQLHealthMetrics -Config $Config
            Write-PodeJsonResponse -Value $Health
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Get alerts
    Add-PodeRoute -Method Get -Path '/api/alerts' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Viewer role or higher can access this
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Viewer')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $Alerts = Get-AXAlerts -Config $Config
            Write-PodeJsonResponse -Value @{ data = $Alerts }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Acknowledge alert
    Add-PodeRoute -Method Post -Path '/api/alerts/:id/acknowledge' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        $AlertId = $WebEvent.Parameters['id']
        
        # Power-User role or higher can acknowledge alerts
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Power-User')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            Set-AXAlertAcknowledged -Config $Config -AlertId $AlertId
            Write-PodeJsonResponse -Value @{ success = $true }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: AI Chat (if enabled)
    if ($PodeContext.Server.Data['EnableOpenAI']) {
        Add-PodeRoute -Method Post -Path '/api/ai/chat' -ScriptBlock {
            $Config = $PodeContext.Server.Data['Config']
            $user = $WebEvent.Data['User']
            $Body = $WebEvent.Data
            
            # Power-User role or higher can use AI features
            if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Power-User')) {
                Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
                return
            }
            
            try {
                $Response = Invoke-AXAIChat -Config $Config -Message $Body.message -Context $Body.context
                Write-PodeJsonResponse -Value @{ response = $Response }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        
        # API: AI Anomaly Detection
        Add-PodeRoute -Method Get -Path '/api/ai/anomalies' -ScriptBlock {
            $Config = $PodeContext.Server.Data['Config']
            $user = $WebEvent.Data['User']
            
            # Power-User role or higher can access AI anomalies
            if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Power-User')) {
                Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
                return
            }
            
            try {
                $Anomalies = Get-AXAIAnomalies -Config $Config
                Write-PodeJsonResponse -Value @{ data = $Anomalies }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
        
        # API: AI Recommendations
        Add-PodeRoute -Method Get -Path '/api/ai/recommendations' -ScriptBlock {
            $Config = $PodeContext.Server.Data['Config']
            $user = $WebEvent.Data['User']
            
            # Power-User role or higher can access AI recommendations
            if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Power-User')) {
                Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
                return
            }
            
            try {
                $Recommendations = Get-AXAIRecommendations -Config $Config
                Write-PodeJsonResponse -Value @{ data = $Recommendations }
            }
            catch {
                Write-PodeJsonResponse -Value @{
                    error = $_.Exception.Message
                } -StatusCode 500
            }
        }
    }
    
    # API: Health check
    Add-PodeRoute -Method Get -Path '/api/health' -ScriptBlock {
        Write-PodeJsonResponse -Value @{
            status = 'healthy'
            timestamp = Get-Date -Format 'o'
            version = '2.0.0'
        }
    }
    
    # ============================================
    # User Management API Routes (Admin only)
    # ============================================
    
    # API: Get all users (Admin only)
    Add-PodeRoute -Method Get -Path '/api/users' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        
        # Admin role required
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Admin')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $users = Get-AXUsers -Config $Config
            Write-PodeJsonResponse -Value @{ data = $users }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Create user (Admin only)
    Add-PodeRoute -Method Post -Path '/api/users' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        $Body = $WebEvent.Data
        
        # Admin role required
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Admin')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $result = New-AXUser -Config $Config -Username $Body.username -Password $Body.password -Role $Body.role -Email $Body.email -FullName $Body.fullName
            Write-PodeJsonResponse -Value @{ success = $result }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Update user (Admin only)
    Add-PodeRoute -Method Put -Path '/api/users/:id' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        $UserId = $WebEvent.Parameters['id']
        $Body = $WebEvent.Data
        
        # Admin role required
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Admin')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $result = Update-AXUser -Config $Config -UserId $UserId -Role $Body.role -Email $Body.email -FullName $Body.fullName -IsActive $Body.isActive
            Write-PodeJsonResponse -Value @{ success = $result }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Delete user (Admin only)
    Add-PodeRoute -Method Delete -Path '/api/users/:id' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $user = $WebEvent.Data['User']
        $UserId = $WebEvent.Parameters['id']
        
        # Admin role required
        if (-not (Test-AXUserPermission -UserRole $user.Role -RequiredRole 'Admin')) {
            Write-PodeJsonResponse -Value @{ error = 'Insufficient permissions' } -StatusCode 403
            return
        }
        
        try {
            $result = Remove-AXUser -Config $Config -UserId $UserId
            Write-PodeJsonResponse -Value @{ success = $result }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # ============================================
    # Authentication API Routes
    # ============================================
    
    # API: Login
    Add-PodeRoute -Method Post -Path '/api/auth/login' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $Body = $WebEvent.Data
        
        try {
            # Validate credentials
            $isValid = Test-AXUserCredentials -Config $Config -Username $Body.username -Password $Body.password
            
            if ($isValid) {
                # Get user details
                $user = Get-AXUser -Config $Config -Username $Body.username
                
                # Create JWT token
                $token = New-AXJWTToken -Config $Config -UserId $user.Id -Username $user.Username -Role $user.Role
                
                # Store token in session for web requests
                $WebEvent.Session.Data['AuthToken'] = $token
                
                Write-PodeJsonResponse -Value @{
                    success = $true
                    token = $token
                    user = @{
                        id = $user.Id
                        username = $user.Username
                        role = $user.Role
                    }
                }
            }
            else {
                Write-PodeJsonResponse -Value @{
                    success = $false
                    error = 'Invalid credentials'
                } -StatusCode 401
            }
        }
        catch {
            Write-PodeJsonResponse -Value @{
                success = $false
                error = $_.Exception.Message
            } -StatusCode 500
        }
    }
    
    # API: Logout
    Add-PodeRoute -Method Post -Path '/api/auth/logout' -ScriptBlock {
        # Clear session token
        $WebEvent.Session.Data.Remove('AuthToken')
        
        Write-PodeJsonResponse -Value @{ success = $true }
    }
    
    # API: Get current user info
    Add-PodeRoute -Method Get -Path '/api/auth/me' -ScriptBlock {
        $user = $WebEvent.Data['User']
        
        Write-PodeJsonResponse -Value @{
            id = $user.UserId
            username = $user.Username
            role = $user.Role
        }
    }
    
    # API: Get available roles (no auth required for this endpoint)
    Add-PodeRoute -Method Get -Path '/api/auth/roles' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        $roles = Get-AXRoles
        
        Write-PodeJsonResponse -Value @{ roles = $roles }
    }
    
    # ============================================
    # Background Schedules
    # ============================================
    
    # Schedule: Collect metrics every 1 minute
    Add-PodeSchedule -Name 'CollectMetrics' -Cron '@minutely' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        try {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [METRICS] Collecting metrics..." -ForegroundColor Cyan
            Invoke-AXMetricsCollection -Config $Config
        }
        catch {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [ERROR] Metrics collection failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Schedule: Check alerts every 2 minutes
    Add-PodeSchedule -Name 'CheckAlerts' -Cron '*/2 * * * *' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        try {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [ALERTS] Checking alerts..." -ForegroundColor Yellow
            Invoke-AXAlertCheck -Config $Config
        }
        catch {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [ERROR] Alert check failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Schedule: Cleanup old data every hour
    Add-PodeSchedule -Name 'CleanupData' -Cron '@hourly' -ScriptBlock {
        $Config = $PodeContext.Server.Data['Config']
        try {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [CLEANUP] Cleaning up old data..." -ForegroundColor Gray
            Invoke-AXDataCleanup -Config $Config
        }
        catch {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [ERROR] Cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Schedule: AI Analysis every 5 minutes (if enabled)
    if ($PodeContext.Server.Data['EnableOpenAI']) {
        Add-PodeSchedule -Name 'AIAnalysis' -Cron '*/5 * * * *' -ScriptBlock {
            $Config = $PodeContext.Server.Data['Config']
            try {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [AI] Running AI analysis..." -ForegroundColor Magenta
                Invoke-AXAIAnalysis -Config $Config
            }
            catch {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')] [ERROR] AI analysis failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}