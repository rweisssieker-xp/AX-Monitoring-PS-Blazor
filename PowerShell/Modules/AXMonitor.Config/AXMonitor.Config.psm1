<#
.SYNOPSIS
    Configuration module for AX Monitor
.DESCRIPTION
    Handles configuration loading, validation, and environment management
#>

# Helper function for null coalescing (PowerShell 5.1 compatible)
function Get-ValueOrDefault {
    param($Value, $Default)
    if ([string]::IsNullOrEmpty($Value)) { return $Default } else { return $Value }
}

function Initialize-AXMonitorConfig {
    <#
    .SYNOPSIS
        Initialize configuration for the specified environment
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('DEV', 'TST', 'PRD')]
        [string]$Environment
    )
    
    $ConfigPath = Join-Path $PSScriptRoot "..\..\Config"
    $EnvFile = Join-Path $ConfigPath ".env.$Environment"
    
    if (-not (Test-Path $EnvFile)) {
        throw "Configuration file not found: $EnvFile"
    }
    
    # Load environment variables
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [System.Environment]::SetEnvironmentVariable($key, $value, 'Process')
        }
    }
    
    # Build configuration object
    $Config = @{
        Environment = $Environment
        
        # AX Database
        AXDatabase = @{
            Server = $env:AX_DB_SERVER
            Database = $env:AX_DB_NAME
            Username = $env:AX_DB_USER
            Password = $env:AX_DB_PASSWORD
            Driver = Get-ValueOrDefault $env:AX_DB_DRIVER 'ODBC Driver 17 for SQL Server'
            ReadOnly = $true
            ConnectionTimeout = 30
            CommandTimeout = 60
        }
        
        # Staging Database
        StagingDatabase = @{
            Server = $env:STAGING_DB_SERVER
            Database = $env:STAGING_DB_NAME
            Username = $env:STAGING_DB_USER
            Password = $env:STAGING_DB_PASSWORD
            Driver = Get-ValueOrDefault $env:STAGING_DB_DRIVER 'ODBC Driver 17 for SQL Server'
        }
        
        # Monitoring Settings
        Monitoring = @{
            CheckIntervalMinutes = [int](Get-ValueOrDefault $env:MONITORING_INTERVAL 5)
            RetentionDays = [int](Get-ValueOrDefault $env:RETENTION_DAYS 90)
            BatchSize = [int](Get-ValueOrDefault $env:BATCH_SIZE 1000)
            
            Thresholds = @{
                CPUPercent = [int](Get-ValueOrDefault $env:THRESHOLD_CPU 80)
                MemoryPercent = [int](Get-ValueOrDefault $env:THRESHOLD_MEMORY 85)
                DiskPercent = [int](Get-ValueOrDefault $env:THRESHOLD_DISK 90)
                BlockingDurationSeconds = [int](Get-ValueOrDefault $env:THRESHOLD_BLOCKING 30)
                DeadlockCount = [int](Get-ValueOrDefault $env:THRESHOLD_DEADLOCK 1)
            }
        }
        
        # Alert Settings
        Alerts = @{
            Email = @{
                Enabled = if ($env:ALERT_EMAIL_ENABLED) { [bool]::Parse($env:ALERT_EMAIL_ENABLED) } else { $true }
                SMTPServer = $env:SMTP_SERVER
                SMTPPort = [int](Get-ValueOrDefault $env:SMTP_PORT 587)
                SMTPUser = $env:SMTP_USER
                SMTPPassword = $env:SMTP_PASSWORD
                FromAddress = $env:SMTP_FROM
                ToAddresses = if ($env:ALERT_RECIPIENTS) { ($env:ALERT_RECIPIENTS -split ',') | ForEach-Object { $_.Trim() } } else { @() }
                UseSSL = if ($env:SMTP_USE_SSL) { [bool]::Parse($env:SMTP_USE_SSL) } else { $true }
            }
            
            Teams = @{
                Enabled = if ($env:ALERT_TEAMS_ENABLED) { [bool]::Parse($env:ALERT_TEAMS_ENABLED) } else { $false }
                WebhookURL = $env:TEAMS_WEBHOOK_URL
            }
            
            Slack = @{
                Enabled = if ($env:ALERT_SLACK_ENABLED) { [bool]::Parse($env:ALERT_SLACK_ENABLED) } else { $false }
                WebhookURL = $env:SLACK_WEBHOOK_URL
            }
        }
        
        # AI/OpenAI Settings
        AI = @{
            Enabled = if ($env:OPENAI_ENABLED) { [bool]::Parse($env:OPENAI_ENABLED) } else { $false }
            APIKey = $env:OPENAI_API_KEY
            Model = Get-ValueOrDefault $env:OPENAI_MODEL 'gpt-4'
            MaxTokens = [int](Get-ValueOrDefault $env:OPENAI_MAX_TOKENS 2000)
            Temperature = [double](Get-ValueOrDefault $env:OPENAI_TEMPERATURE 0.7)
            
            Features = @{
                AnomalyDetection = if ($env:AI_ANOMALY_DETECTION) { [bool]::Parse($env:AI_ANOMALY_DETECTION) } else { $true }
                PredictiveAnalysis = if ($env:AI_PREDICTIVE) { [bool]::Parse($env:AI_PREDICTIVE) } else { $true }
                ChatAssistant = if ($env:AI_CHAT) { [bool]::Parse($env:AI_CHAT) } else { $true }
                AutoRemediation = if ($env:AI_AUTO_REMEDIATION) { [bool]::Parse($env:AI_AUTO_REMEDIATION) } else { $false }
            }
        }
        
        # Logging
        Logging = @{
            Level = Get-ValueOrDefault $env:LOG_LEVEL 'INFO'
            Path = if ($env:LOG_PATH) { $env:LOG_PATH } else { Join-Path $PSScriptRoot "..\..\Logs" }
            MaxSizeMB = [int](Get-ValueOrDefault $env:LOG_MAX_SIZE_MB 100)
            MaxFiles = [int](Get-ValueOrDefault $env:LOG_MAX_FILES 10)
        }
        
        # Security
        Security = @{
            SecretKey = $env:SECRET_KEY
            JWTSecret = $env:JWT_SECRET
            SessionTimeoutMinutes = [int](Get-ValueOrDefault $env:SESSION_TIMEOUT 60)
            EnableAuthentication = if ($env:ENABLE_AUTH) { [bool]::Parse($env:ENABLE_AUTH) } else { $false }
        }
    }
    
    # Validate required settings
    $RequiredSettings = @(
        @{ Path = 'AXDatabase.Server'; Name = 'AX_DB_SERVER' }
        @{ Path = 'AXDatabase.Database'; Name = 'AX_DB_NAME' }
        @{ Path = 'StagingDatabase.Server'; Name = 'STAGING_DB_SERVER' }
        @{ Path = 'StagingDatabase.Database'; Name = 'STAGING_DB_NAME' }
    )
    
    foreach ($setting in $RequiredSettings) {
        $value = Get-ConfigValue -Config $Config -Path $setting.Path
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw "Required configuration missing: $($setting.Name)"
        }
    }
    
    return $Config
}

function Get-ConfigValue {
    <#
    .SYNOPSIS
        Get a configuration value by path (e.g., 'AXDatabase.Server')
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Path
    )
    
    $parts = $Path -split '\.'
    $current = $Config
    
    foreach ($part in $parts) {
        if ($current -is [hashtable] -and $current.ContainsKey($part)) {
            $current = $current[$part]
        }
        else {
            return $null
        }
    }
    
    return $current
}

function Get-AXConnectionString {
    <#
    .SYNOPSIS
        Build SQL Server connection string
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$DatabaseConfig,
        
        [Parameter()]
        [switch]$UseTrustedConnection
    )
    
    $builder = @(
        "Driver={$($DatabaseConfig.Driver)}"
        "Server=$($DatabaseConfig.Server)"
        "Database=$($DatabaseConfig.Database)"
    )
    
    if ($UseTrustedConnection) {
        $builder += "Trusted_Connection=yes"
    }
    else {
        if ($DatabaseConfig.Username -and $DatabaseConfig.Password) {
            $builder += "Uid=$($DatabaseConfig.Username)"
            $builder += "Pwd=$($DatabaseConfig.Password)"
        }
        else {
            $builder += "Trusted_Connection=yes"
        }
    }
    
    if ($DatabaseConfig.ConnectionTimeout) {
        $builder += "Connection Timeout=$($DatabaseConfig.ConnectionTimeout)"
    }
    
    return ($builder -join ';')
}

function Test-AXMonitorConfig {
    <#
    .SYNOPSIS
        Validate configuration
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $errors = @()
    
    # Validate database settings
    if ([string]::IsNullOrWhiteSpace($Config.AXDatabase.Server)) {
        $errors += "AX Database Server is required"
    }
    
    if ([string]::IsNullOrWhiteSpace($Config.AXDatabase.Database)) {
        $errors += "AX Database Name is required"
    }
    
    # Validate alert settings if enabled
    if ($Config.Alerts.Email.Enabled) {
        if ([string]::IsNullOrWhiteSpace($Config.Alerts.Email.SMTPServer)) {
            $errors += "SMTP Server is required when email alerts are enabled"
        }
        
        if ($Config.Alerts.Email.ToAddresses.Count -eq 0) {
            $errors += "At least one alert recipient is required"
        }
    }
    
    # Validate AI settings if enabled
    if ($Config.AI.Enabled) {
        if ([string]::IsNullOrWhiteSpace($Config.AI.APIKey)) {
            $errors += "OpenAI API Key is required when AI features are enabled"
        }
    }
    
    if ($errors.Count -gt 0) {
        throw "Configuration validation failed:`n" + ($errors -join "`n")
    }
    
    return $true
}

function Export-AXMonitorConfig {
    <#
    .SYNOPSIS
        Export configuration to JSON file
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Path
    )
    
    # Remove sensitive data
    $SafeConfig = $Config.Clone()
    $SafeConfig.AXDatabase.Password = '***'
    $SafeConfig.StagingDatabase.Password = '***'
    $SafeConfig.Alerts.Email.SMTPPassword = '***'
    $SafeConfig.AI.APIKey = '***'
    
    $SafeConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $Path -Encoding UTF8
}

# Export module members
Export-ModuleMember -Function @(
    'Initialize-AXMonitorConfig'
    'Get-ConfigValue'
    'Get-AXConnectionString'
    'Test-AXMonitorConfig'
    'Export-AXMonitorConfig'
)
